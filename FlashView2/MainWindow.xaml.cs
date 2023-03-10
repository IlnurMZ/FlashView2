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
            if (openFileDialog.ShowDialog() == true)
            {           
                string pathFlash = openFileDialog.FileName;
                nameFile = openFileDialog.SafeFileName;
                try
                {
                    // считываем данные флеш-файла
                    FlashFile = File.ReadAllBytes(pathFlash);
                    FlashFile = FlashFile.Skip(384).ToArray();
                    // считываем данные конфиг-файла
                    string pathConfig = "Configurations\\flashRead_NNGK_v1_07-12-2022.cfg";
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
            Percent = 0;
            Packets = packets;
            ID_Device = FlashFile[1];
            ID_Packet = FlashFile[0];            
            UpdateTable(FlashFile, packets);          
            //AppViewModel = new ApplicationViewModel(FlashFile, packets, nameFile);            
            //DataContext = AppViewModel;
            //AppViewModel.StatusMainWindow += $"Начинается загрузка файла {openFileDialog.FileName}\n";
            // делаем привязку кнопки формирования las-файла к переменной IsLasFile
            //Binding binding = new Binding();
            //binding.ElementName = "IsLasFile";
            //binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            //menuButtonFormLas.SetBinding(MenuItem.IsEnabledProperty, binding);
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
        }
        string CalculateValueByType(string typeCalc, string value, double[] data) // по типу вычисления выдаем результат
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
                        result = (data[0] * doubleValue + data[1]).ToString();
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

        System.Data.DataTable LoadDataTable(List<Packet> packets, byte[] flash)
        {
            DataTable myTable = new DataTable();

            foreach (var item in packets[0].HeaderColumns)
            {
                myTable.Columns.Add(item);
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
            int countBadByte = 0;
            DataRow row;
            byte loadStatus = 0;
            byte tempVal;

            for (int i = 0; i < flash.Length; i++) // FlashFile.Length; i++)
            {
                // условие захода в начало строки
                bool isGoodStartLine = flash[i] == idPacketArray && flash[i + 1] == idDeviceArray;

                if (i + countByteRow > flash.Length) // проверка завершенности строки, чтобы исключить выход за пределы массива байт
                {
                    break;
                }
                // проверка двух байт на конец строки
                bool isGoodEndLine = flash[i + countByteRow - 2] == endLinePacket[0] && flash[i + countByteRow - 1] == endLinePacket[1];
                try
                {
                    if (isGoodStartLine && isGoodEndLine) // проверка совпадения на начало строки
                    {
                        row = myTable.NewRow(); // создаем строку для таблицы
                        for (int j = 0; j < countParams; j++)
                        {
                            byte countByte = myPacket.LengthParams[j]; // определяем количество байт на параметр
                            byte[] values = new byte[countByte]; // берем необходимое количество байт                   
                            Array.Copy(flash, i, values, 0, countByte); // копируем наш кусок

                            string valueA = GetValueByType(myPacket.TypeParams[j], values); // вычисляем значение по типу данных
                            string valueB = CalculateValueByType(myPacket.TypeCalculate[j], valueA, myPacket.DataCalculation[j]); // вычисляем пересчет данного по типу
                            row[j] = valueB;
                            i += countByte; // смещаем курсор по общему массиву байт
                        }
                        i--;
                        myTable.Rows.Add(row);
                    }
                    else
                    {
                        countBadByte++;
                    }
                }
                catch (FormatException)
                {
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

        private void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
           
            ToA();
        }



        public void ToA(string excelFilePath = null)
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

    }
}
