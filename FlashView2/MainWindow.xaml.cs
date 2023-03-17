using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using DataTable = System.Data.DataTable;
using Window = System.Windows.Window;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public byte[]? FlashFile { get; set; }
        private string statusMainWindow;

        public string StatusMainWindow
        {
            get
            {
                return statusMainWindow;
            }
            set
            {
                statusMainWindow = value;
                OnPropertyChanged("StatusMainWindow");
            }
        }

        private bool isLasFile;
        public bool IsLasFile
        {
            get
            {
                return isLasFile;
            }
            set
            {
                isLasFile = value;
                OnPropertyChanged("IsLasFile");
            }
        }

              
        private int percent; // проценты загрузки для прогресбара
        public int Percent
        {
            get
            {
                return percent;
            }
            set
            {
                percent = value;
                OnPropertyChanged("Percent");
            }
        }

        private DataTable dataTable; // таблица для datagrid1  
        public DataTable DataTable
        {
            get
            {
                return dataTable;
            }
            set
            {
                dataTable = value;
                OnPropertyChanged("DataTable");
            }
        }
        public byte ID_Device { get; set; }
        public byte ID_Packet { get; set; }
        public List<Packet> Packets { get; set; }
        //public ApplicationViewModel AppViewModel { get; set; }
        LasMenuForm _lasMenuForm;   

        public MainWindow()
        {
            InitializeComponent();           
            DataContext = this;
            IsLasFile = false;
        }

        public void MenuItemOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Flash Files|*.fl";
            openFileDialog.Title = "Выберите flash-файл с данными";
            List<List<string>> dataConfig = new List<List<string>>();
            string nameFile;
            string pathConfig;
            if (openFileDialog.ShowDialog() == true)
            {           
                string pathFlash = openFileDialog.FileName;
                nameFile = openFileDialog.SafeFileName;
                try
                {
                    // считываем данные флеш-файла
                    FlashFile = File.ReadAllBytes(pathFlash);
                    FlashFile = FlashFile.Skip(384).ToArray();
                    ID_Device = FlashFile[1];
                    ID_Packet = FlashFile[0];
                    // считываем данные конфиг-файла
                    //string pathConfig = "Configurations\\flashRead_NNGK_v1_07-12-2022.cfg";
                    pathConfig = GetPathConfigByParams(ID_Device, ID_Packet);
                    char[] separators = { ' ', '\t' };
                    using (var reader = new StreamReader(pathConfig))
                    {
                        while (!reader.EndOfStream)
                        {
                            var row = reader.ReadLine();

                            if (!string.IsNullOrWhiteSpace(row))
                            {
                                string[] line = row.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                dataConfig.Add(new List<string>(line));
                            }
                        }
                    }                    
                }
                catch (Exception ex)
                {
                    StatusMainWindow += $"{DateTime.Now}: Возникла ошибка: {ex.Message}\n";
                    return;
                }
            }
            else
            {
                return;
            }

            List<Packet> packets = HandleConfigData(dataConfig);
            StatusMainWindow += $"{DateTime.Now}: Загрузка файла началась: {nameFile} \n";
            txtBoxStatus.ScrollToEnd();
            Percent = 0;
            Packets = packets;
            //ID_Device = FlashFile[1];
            //ID_Packet = FlashFile[0];            
            UpdateTable(FlashFile, packets);
            txtBoxStatus.ScrollToEnd();
            //AppViewModel = new ApplicationViewModel(FlashFile, packets, nameFile);            
            //DataContext = AppViewModel;
            //AppViewModel.StatusMainWindow += $"Начинается загрузка файла {openFileDialog.FileName}\n";
            // делаем привязку кнопки формирования las-файла к переменной IsLasFile
            //Binding binding = new Binding();
            //binding.ElementName = "IsLasFile";
            //binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            //menuButtonFormLas.SetBinding(MenuItem.IsEnabledProperty, binding);
        }

        private string GetPathConfigByParams(byte iD_Device, byte iD_Packet)
        {            
            string result = "";
            result = (iD_Device, ID_Packet) switch
            {
                (6,1) => "Configurations\\flashRead_NNGK_v1_07-12-2022.cfg",
                _=> throw new Exception("Конфиг.файл не найден")
            };
            return result;            
        }

        List<Packet> HandleConfigData(List<List<string>> dataConfig)
        {
            // расфасовываем данные нужным образом          
            string idDevice = "";
            string idPacket = "";
            string version = dataConfig[0][0];
            List<List<string>> DeviceParam = new(); // переработанный массив для выгрузки данных

            for (int i = 1; i < dataConfig.Count; i++)
            {
                for (int j = 0; j < dataConfig[i].Count; j++)
                {

                    if (dataConfig[i][0][0] == '@' && dataConfig[i + 2][0][0] == '@') // вычисляем id устройства, обрамленное @
                    {
                        idDevice = dataConfig[i + 1][0];
                        i = i + 2;
                        break;
                    }

                    if (dataConfig[i][j][0] == '~' && idDevice != "") // вычисляем id пакета
                    {
                        idPacket = dataConfig[i][j].Trim('~');
                        do
                        {
                            i++;
                            List<string> data = new();
                            data.Add(idPacket);
                            data.Add(idDevice);
                            if (dataConfig[i][0][0] == '*')
                            {
                                dataConfig[i].RemoveAt(0);
                            }
                            else
                            {
                                throw new Exception("Ошибка описания строки данных flash");                                
                            }
                            data.AddRange(dataConfig[i]);
                            DeviceParam.Add(new List<string>(data));
                            if (dataConfig[i + 1][0][0] == '#') // записываем конец строки, удаляем лишние символы
                            {
                                dataConfig[i + 1].RemoveAll(x => x == "#" || x == "H" || x == "h"); // удаляем лишние элементы из Листа
                                List<string> str = dataConfig[i + 1];
                                DeviceParam.Add(new List<string>(str));
                                break;
                            }
                        } while (true);
                        i++;
                    }
                }
            }

            // далее идет переработка фассованных данных и создание пакетов
            List<Packet> PacketsSettings = new();

            for (int i = 0; i < DeviceParam.Count; i++)
            {
                var packet = new Packet();

                try
                {
                    packet.ID_Packet = byte.Parse(DeviceParam[i][0]);
                    packet.ID_Device = byte.Parse(DeviceParam[i][1]);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    throw new Exception(ex.Message);
                }

                do
                {
                    var list = DeviceParam[i];
                    byte length = list[2] switch
                    {
                        "byteUs" => 1,
                        "byteS" => 1,
                        "shortUs" => 2,
                        "shortS" => 2,
                        "intS" => 4,
                        "intUs" => 4,
                        "bdTime" => 6,
                        _ => 0
                    };
                    if (length == 0)
                    {                        
                        throw new Exception("Неопознанное обозначение типа данных в конф.файле");
                    }
                    packet.LengthLine += length;
                    packet.LengthParams.Add(length);
                    packet.TypeParams.Add(list[2]);
                    if (list[5] == "[]")
                    {
                        packet.HeaderColumns.Add(list[4]);
                    }
                    else
                    {
                        packet.HeaderColumns.Add($"{list[4]} {list[5]}");
                    }

                    // пропускаем значение неопределенности
                    packet.TypeCalculate.Add(list[7]);
                    double[] data = new double[4];

                    for (int j = 8; j <= 11; j++)
                    {
                        bool isParseDouble = double.TryParse(list[j], NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
                        if (!isParseDouble)
                        {                            
                            MessageBox.Show("Ошибка парсинга чисел для пересчета данного");                            
                        }
                        data[j - 8] = value;
                    }
                    packet.DataCalculation.Add(data); // загоняем коэффициенты для пересчета
                    bool isCountWidth = byte.TryParse(list[12], out byte resultCount);
                    bool isParseWidth = byte.TryParse(list[13], out byte resultWindth);

                    if (!isParseWidth && !isCountWidth)
                    {                        
                        throw new Exception("Ошибка парсинга чисел для пересчета данного");
                    }
                    packet.CountSign.Add(resultCount);
                    packet.WidthColumn.Add(resultWindth);
                    i++;
                } while (DeviceParam[i].Count != 2 && DeviceParam[i].Count != 0);

                if (DeviceParam[i].Count == 2)
                {
                    try
                    {
                        byte one = byte.Parse(DeviceParam[i][0]);
                        byte two = byte.Parse(DeviceParam[i][1]);
                        packet.endLine[0] = one;
                        packet.endLine[1] = two;
                    }
                    catch (Exception ex)
                    {                        
                        throw new Exception("Не удалось сконвертировать конец строки в байты.\n" + ex.Message);
                    }

                }
                else if (DeviceParam[i].Count == 0)
                {
                    packet.endLine[0] = 0;
                    packet.endLine[0] = 0;
                }               
                PacketsSettings.Add(packet);
            }
            
            return PacketsSettings;
        }

        void MenuItemCloseProgram_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        void r2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {            
            if (e.PropertyName.Contains('[') || e.PropertyName.Contains(']') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            }
        }

        private void menuButtonFormLas_Click(object sender, RoutedEventArgs e)
        {            
            List<string> abc = new List<string>();
            var dataRows = dataTable.Rows;
            _lasMenuForm = new LasMenuForm(dataRows);
            _lasMenuForm.Owner = this;
            _lasMenuForm.Show();            
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        async void UpdateTable(byte[] flash, List<Packet> packets)
        {
            await Task.Run(() =>
            {
                DataTable = LoadDataTable(packets, flash);
                IsLasFile = true;
                StatusMainWindow += $"{DateTime.Now}: Загрузка завершена!\n";                
            });
            //txtBoxStatus.ScrollToEnd();
        }
        string CalculateValueByType(string typeCalc, string value, double[] data, byte countSign) // по типу вычисления выдаем результат
        {
            string result = "";
            switch (typeCalc) // смотрим тип вычисления
            {
                case "нет":
                case "вр":
                    result = value;
                    break;
                case "лин":
                    if (double.TryParse(value, out double doubleValue))
                    {
                        if (countSign > 0)
                        {
                            result = Math.Round((data[0] * doubleValue + data[1]), countSign).ToString();
                        }
                        else
                        {
                            result = (data[0] * doubleValue + data[1]).ToString();
                        }
                    }
                    else
                    {
                        throw new Exception("не удалось преобразовать данное при лин.вычислении");
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        string GetValueByType(string typeValue, byte[] value)
        {
            string result = "";
            switch (typeValue)
            {
                case "byteS":
                    result = ((sbyte)(value[0])).ToString();
                    break;
                case "byteUs":
                    result = ((byte)(value[0])).ToString();
                    break;
                case "shortS":
                    result = ((short)(value[0] | (value[1] << 8))).ToString();
                    break;
                case "shortUs":
                    result = ((ushort)(value[0] | (value[1] << 8))).ToString();
                    break;
                //case "intS":
                //    break;
                //case "intUs":
                //    break;
                case "bdTime":
                    result = GetbdTime(value);
                    break;
                default:
                    throw new FormatException("неизвестный тип данных");
            }
            return result;
        }

        string GetbdTime(Byte[] array)
        {
            string dateTime = Convert.ToHexString(array);
            string second = "";
            string minute = "";
            string hour = "";
            string day = "";
            string month = "";
            string year = "";
            string date;

            second = dateTime[0].ToString() + dateTime[1].ToString();
            minute = dateTime[2].ToString() + dateTime[3].ToString();
            hour = dateTime[4].ToString() + dateTime[5].ToString();
            day = dateTime[6].ToString() + dateTime[7].ToString();
            month = dateTime[8].ToString() + dateTime[9].ToString();
            year = dateTime[10].ToString() + dateTime[11].ToString();
            //date = new DateTime(year, month, day, hour, minute, second);
            date = $"{hour}:{minute}:{second} {day}.{month}.{year}";
            if (!DateTime.TryParse(date, out DateTime timeTest))
            {
                throw new FormatException("Ошибка формата времени");
            }
            return date;
        }

        DataTable LoadDataTable(List<Packet> packets, byte[] flash)
        {
            DataTable myTable = new DataTable();

            myTable.Columns.Add("N");            
            foreach (var item in packets[0].HeaderColumns)
            {
                string[] splitHeader = item.Split('/');
                if (splitHeader.Length == 2)
                {
                    myTable.Columns.Add(splitHeader[0] + "\\" + "\n" + splitHeader[1]);
                }
                else
                {
                    myTable.Columns.Add(item);
                }                
            }
            
            int countColumn = packets[0].HeaderColumns.Count;

            // вычисляем изначальные id пакета и устройства
            byte idPacketArray = flash[0];
            byte idDeviceArray = flash[1];
            // выбираем нужный пакет исходя из id-шников 
            var myPacket = packets[0];
            byte[] endLinePacket = myPacket.endLine;
            int countByteRow = myPacket.LengthLine; // количество байт на строку
            byte countParams = (byte)myPacket.TypeParams.Count; // количество столбцов            
            DataRow row;
            byte loadStatus = 0;
            byte tempVal;
            //int countRows = 0;
            int countBadBites = 0;
            int countBadTimes = 0;
            bool isBadLine = false;
            bool isBadTime = false;

            for (int i = 0; i < flash.Length; i++) 
            {
                // условие захода в начало строки
                bool isGoodStartLine = flash[i] == idPacketArray && flash[i + 1] == idDeviceArray;

                if (i + countByteRow > flash.Length) // проверка завершенности строки, чтобы исключить выход за пределы массива байт
                {
                    if (isBadTime)
                    {
                        StatusMainWindow += $"{DateTime.Now}: Ошибка данных (не удалось определить время), после строки {myTable.Rows.Count}, количество строк: {countBadTimes}\n";
                        isBadTime = false;
                        countBadTimes = 0;
                    }
                    countBadBites += flash.Length - i;
                    StatusMainWindow += $"{DateTime.Now}: Ошибка конца файла, после строки {myTable.Rows.Count}, количество ошибочных байт: {countBadBites}\n";
                    break;
                }
                // проверка двух байт на конец строки
                bool isGoodEndLine = flash[i + countByteRow - 2] == endLinePacket[0] && flash[i + countByteRow - 1] == endLinePacket[1];
                try
                {
                    if (isGoodStartLine && isGoodEndLine) // проверка совпадения на начало и конец строки
                    {
                        if (isBadLine)
                        {
                            StatusMainWindow += $"{DateTime.Now}: Ошибка после {myTable.Rows.Count} строки, количество ошибочных байт: {countBadBites}\n";
                            countBadBites = 0;
                            isBadLine = false;
                        } 

                        row = myTable.NewRow(); // создаем строку для таблицы
                        for (int j = 0; j < countParams + 1; j++) // добавил счетчик строк
                        {
                            if (j == 0)
                            {
                                row[j] = " " + (myTable.Rows.Count + 1).ToString() + " ";                                
                                continue;
                            }
                            byte countByte = myPacket.LengthParams[j-1]; // определяем количество байт на параметр
                            byte[] values = new byte[countByte]; // берем необходимое количество байт                   
                            Array.Copy(flash, i, values, 0, countByte); // копируем наш кусок

                            string valueA = GetValueByType(myPacket.TypeParams[j-1], values); // вычисляем значение по типу данных
                            string valueB = CalculateValueByType(myPacket.TypeCalculate[j - 1], valueA, myPacket.DataCalculation[j - 1], myPacket.CountSign[j-1]); // вычисляем пересчет данного по типу
                            row[j] = " " + valueB + " ";                            
                            i += countByte; // смещаем курсор по общему массиву байт                            
                        }

                        if (isBadTime)
                        {
                            StatusMainWindow += $"{DateTime.Now}: Ошибка данных (не удалось определить время), после строки {myTable.Rows.Count}, количество строк: {countBadTimes}\n";
                            isBadTime = false;
                            countBadTimes = 0;
                        }
                        i--;
                        myTable.Rows.Add(row);                     
                    }
                    else
                    {
                        isBadLine = true;
                        countBadBites++;
                    }
                }
                catch (FormatException)
                {
                    isBadTime = true;
                    countBadTimes++;
                    i += 29;
                    continue;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    continue;
                }
                tempVal = (byte)(i * 1.0 / flash.Length * 100);
                if (tempVal >= loadStatus)
                {
                    loadStatus = (byte)(tempVal + 10);
                    Percent = loadStatus;
                }
            }
            Percent = 0;
            return myTable;
        }

        public void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            string path;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "excel files|*.xlsx";
            saveFileDialog.Title = "Сохранение";
            if (saveFileDialog.ShowDialog() == true)
            {
                path = saveFileDialog.FileName;
                StatusMainWindow += $"{DateTime.Now}: Выполняется экспорт данных в формат .xlsx\n";
                txtBoxStatus.ScrollToEnd();
                FastExportToExcelAsync(path);
                txtBoxStatus.ScrollToEnd();
            }                                    
        }

        async void FastExportToExcelAsync(string path)
        {
            await Task.Run(() =>
            {
                FastDtToExcel(path);
                StatusMainWindow += $"{DateTime.Now}: Экспорт данных в формат .xlsx завершен!\n";
            });
            txtBoxStatus.ScrollToEnd();
        }

        async void FastExportToTxtAsync(string path)
        {
            await Task.Run(() =>
            {
                FastExportToTxt(path);
                StatusMainWindow += $"{DateTime.Now}: Экспорт данных в формат .txt завершен!\n";
            });
            txtBoxStatus.ScrollToEnd();
        }

        // работает быстрее чем ExportToExcel, но жрет много памяти
        public void ExportToExcel2()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(DataTable,"Sample Sheet");                
                workbook.SaveAs("HelloWorld.xlsx");
            }                       
        }
        // очень долгий метод сохранения данных в Excel
        public void ExportToExcel(string excelFilePath = null)
        {
            try
            {
                if (DataTable == null || DataTable.Columns.Count == 0)
                    throw new Exception("ExportToExcel: Null or empty input table!\n");

                // load excel, and create a new workbook
                var excelApp = new Microsoft.Office.Interop.Excel.Application();
                excelApp.Workbooks.Add();

                // single worksheet
                Microsoft.Office.Interop.Excel._Worksheet workSheet = (_Worksheet)excelApp.ActiveSheet;

                // column headings
                for (var i = 0; i < DataTable.Columns.Count; i++)
                {
                    workSheet.Cells[1, i + 1] = DataTable.Columns[i].ColumnName.ToString();
                }

                // rows
                for (var i = 0; i < 102; i++)//dataTable.Rows.Count; i++)
                {
                    // to do: format datetime values before printing
                    for (var j = 0; j < dataTable.Columns.Count; j++)
                    {
                        workSheet.Cells[i + 2, j + 1] = dataTable.Rows[i][j].ToString();
                    }
                }

                // check file path
                if (!string.IsNullOrEmpty(excelFilePath))
                {
                    try
                    {
                        workSheet.SaveAs(excelFilePath);
                        excelApp.Quit();
                        MessageBox.Show("Excel file saved!");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("ExportToExcel: Excel file could not be saved! Check filepath.\n"
                                            + ex.Message);
                    }
                }
                else
                { // no file path is given
                    excelApp.Visible = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ExportToExcel: \n" + ex.Message);
            }
        }
        // работает быстро и оптимально (пока)
        public void FastDtToExcel(string excelFilePath)
        {            
            int firstRow = 1;
            int firstCol = 1;
            int lastRow = DataTable.Rows.Count;
            int lastCol = DataTable.Columns.Count;

            var excelApp = new Microsoft.Office.Interop.Excel.Application();
            excelApp.Workbooks.Add();

            // single worksheet
            _Worksheet workSheet = (_Worksheet)excelApp.ActiveSheet;

            Microsoft.Office.Interop.Excel.Range top = workSheet.Cells[firstRow, firstCol];
            Microsoft.Office.Interop.Excel.Range bottom = workSheet.Cells[lastRow, lastCol];
            Microsoft.Office.Interop.Excel.Range all = workSheet.get_Range(top, bottom);
            string[,] arrayDT = new string[DataTable.Rows.Count + 1, DataTable.Columns.Count]; // данные плюс заголовки данных

            for (var i = 0; i < DataTable.Columns.Count; i++)
            {
                arrayDT[0, i] = DataTable.Columns[i].ColumnName;                
            }
            byte loadStatus = 0;
            byte tempVal;
            int countRows = DataTable.Rows.Count;
            //loop rows and columns
            for (int i = 1; i < DataTable.Rows.Count; i++)
            {
                for (int j = 0; j < DataTable.Columns.Count; j++)
                {
                    arrayDT[i, j] = DataTable.Rows[i][j].ToString();
                }

                tempVal = (byte)(i * 1.0 / countRows * 100);
                if (tempVal >= loadStatus)
                {
                    loadStatus = (byte)(tempVal + 10);
                    Percent = loadStatus;
                }
            }                    

            //insert value in worksheet
            all.Value2 = arrayDT;
            Percent = 0;
            // check file path
            if (!string.IsNullOrEmpty(excelFilePath))
            {
                try
                {
                    workSheet.SaveAs(excelFilePath);
                    excelApp.Quit();
                    //MessageBox.Show("Excel file saved!");
                }
                catch (Exception ex)
                {
                    //throw new Exception("ExportToExcel: Excel file could not be saved! Check filepath.\n"
                    //                    + ex.Message);
                    StatusMainWindow += $"{DateTime.Now}: {ex.Message} \n";
                }
            }
            else
            { // no file path is given
                excelApp.Visible = true;
            }

        }
        public void FastExportToTxt(string txtFilePath)
        {            
            byte loadStatus = 0;
            byte tempVal;
            int countRows = DataTable.Rows.Count;
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                sb.Append(DataTable.Columns[0].ColumnName + "   ");
            }

            for (int i = 0; i < DataTable.Rows.Count; i++)
            {                
                for (int j = 0; j < DataTable.Columns.Count; j++)
                {
                    sb.Append(DataTable.Rows[i][j].ToString() + "   ");
                }
                sb.AppendLine();

                tempVal = (byte)(i * 1.0 / countRows * 100);
                if (tempVal >= loadStatus)
                {
                    loadStatus = (byte)(tempVal + 10);
                    Percent = loadStatus;
                }
            }

            try
            {
                using (var writer = new StreamWriter(txtFilePath, true))
                {
                    writer.WriteLine(sb.ToString());
                }
            }
            catch (Exception exception)
            {
                StatusMainWindow += $"{DateTime.Now}: {exception.Message} \n";
            }
            Percent = 0;
        }
        private void btnSaveTxT_Click(object sender, RoutedEventArgs e)
        {
            string path;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "txt files|*.txt";
            saveFileDialog.Title = "Сохранение";

            if (saveFileDialog.ShowDialog() == true)
            {
                path = saveFileDialog.FileName;
                StatusMainWindow += $"{DateTime.Now}: Выполняется экспорт данных в формат .txt\n";
                txtBoxStatus.ScrollToEnd();
                FastExportToTxtAsync(path);
                txtBoxStatus.ScrollToEnd();
            }            
        }
       
    }
}
