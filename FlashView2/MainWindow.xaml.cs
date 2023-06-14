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
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Collections.ObjectModel;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //public byte[]? FlashFile { get; set; }
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

        private List<double> depth; // проценты загрузки для прогресбара
        public List<double> Depth
        {
            get
            {
                return depth;
            }
            set
            {
                depth = value;
                OnPropertyChanged("Depth");
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
        LasMenuForm _lasMenuForm;        
        List<ConfFileInfo> confFilesInfo;
        Packet mainPacket;


        public MainWindow()
        {
            InitializeComponent();           
            DataContext = this;
            IsLasFile = false;            
            confFilesInfo = new List<ConfFileInfo>();            
            try
            {
                LoadSeachInfo();
            }
            catch
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void MenuItemOpenFile_Click(object sender, RoutedEventArgs e)
        {
            byte[]? FlashFile;
            mainPacket = new Packet();
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
                    string secachPathCong = "";
                    for (int i = 0; i < FlashFile.Length; i++)
                    {
                        foreach (var cf in confFilesInfo)
                        {
                            if (i + cf.LengthLine + 1< FlashFile.Length)
                            {
                                bool isStart = cf.StartBytes[0] == FlashFile[i] && cf.StartBytes[1] == FlashFile[i + 1];
                                bool isEnd = false;
                                // если нет конца строки
                                if (cf.EndBytes[0] == 0 && cf.EndBytes[1] == 0)
                                {
                                    isEnd = cf.StartBytes[0] == FlashFile[i + cf.LengthLine] && cf.StartBytes[1] == FlashFile[i + cf.LengthLine + 1];
                                }
                                else
                                {
                                    isEnd = cf.EndBytes[0] == FlashFile[i + cf.LengthLine - 2] && cf.EndBytes[1] == FlashFile[i + cf.LengthLine - 1];
                                }                                
                                
                                if (isStart && isEnd)
                                {
                                    secachPathCong = cf.PathFile;                                    
                                    mainPacket.ID_Packet = cf.StartBytes[0];
                                    mainPacket.ID_Device = cf.StartBytes[1];
                                    mainPacket.endLine[0] = cf.EndBytes[0];
                                    mainPacket.endLine[1] = cf.EndBytes[1];
                                    break;
                                }
                            }
                        }
                        if (mainPacket.ID_Device != 0)
                        {
                            break;
                        }
                    }

                    if (mainPacket.ID_Device == 0)
                    {
                        ScrollStatusLasTextBox("Возникла ошибка: подходящий конфигурационный файл не найден");
                        //StatusMainWindow+= $"{DateTime.Now}: Возникла ошибка: подходящий конфигурационный файл не найден\n";
                        return;
                    }

                    if (!string.IsNullOrEmpty(secachPathCong))
                    {
                        //считываем данные конфиг - файла
                        
                        char[] separators = { ' ', '\t' };
                        using (var reader = new StreamReader(secachPathCong))
                        {
                            bool isStartData = false;
                            while (!reader.EndOfStream)
                            {
                                var row = reader.ReadLine();

                                if (!string.IsNullOrWhiteSpace(row))
                                {
                                    if (!isStartData && row.Contains($"~{mainPacket.ID_Packet}"))
                                    {
                                        isStartData = true;
                                    }
                                    else if (isStartData)
                                    {
                                        if (row.StartsWith('#'))
                                        {
                                            break;
                                        }
                                        string[] line = row.TrimStart('*').Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                        dataConfig.Add(new List<string>(line));
                                    }                                   
                                }
                            }
                        }
                    }                    

                }
                catch (Exception ex)
                {
                    ScrollStatusLasTextBox($"Возникла ошибка: {ex.Message}");
                    //StatusMainWindow += $"{DateTime.Now}: Возникла ошибка: {ex.Message}\n";
                    return;
                }
            }
            else
            {
                return;
            }
            
            HandleConfigData(dataConfig);
            ScrollStatusLasTextBox($"Загрузка файла началась: {nameFile}");
            //StatusMainWindow += $"{DateTime.Now}: Загрузка файла началась: {nameFile} \n";
            txtBoxStatus.ScrollToEnd();
            Percent = 0;                     
            UpdateTable(FlashFile, mainPacket);
            txtBoxStatus.ScrollToEnd();
        }

        // Обработка конф данных
        void HandleConfigData(List<List<string>> dataConfig)
        {
            for (int i = 0; i < dataConfig.Count; i++)
            {                
                var list = dataConfig[i];
                byte length = list[0] switch
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
                mainPacket.LengthLine += length;
                mainPacket.LengthParams.Add(length);
                mainPacket.TypeParams.Add(list[0]);
                if (list[3] == "[]")
                {
                    mainPacket.HeaderColumns.Add(list[2]); //.Trim('[', ']')
                }
                else
                {
                    mainPacket.HeaderColumns.Add($"{list[2]} {list[3]}"); // .Trim('[', ']')
                }

                // пропускаем значение неопределенности
                mainPacket.TypeCalculate.Add(list[5]);
                double[] data = new double[4];

                for (int j = 6; j <= 9; j++)
                    {
                        bool isParseDouble = double.TryParse(list[j], NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
                        if (!isParseDouble)
                        {                            
                            MessageBox.Show("Ошибка парсинга чисел для пересчета данного");                            
                        }
                        data[j - 6] = value;
                    }
                mainPacket.DataCalculation.Add(data); // загоняем коэффициенты для пересчета
                    bool isCountWidth = byte.TryParse(list[10], out byte resultCount);
                    bool isParseWidth = byte.TryParse(list[11], out byte resultWindth);

                    if (!isParseWidth && !isCountWidth)
                    {                        
                        throw new Exception("Ошибка парсинга чисел для пересчета данного");
                    }
                mainPacket.CountSign.Add(resultCount);
                mainPacket.WidthColumn.Add(resultWindth);                   
            }           
        }

        // Кнопка Выйти
        void MenuItemCloseProgram_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // Обработка заголовков таблицы
        void r2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {            
            if (e.PropertyName.Contains('[') || e.PropertyName.Contains(']') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            }
        }

        // кнопка LAS
        private void menuButtonFormLas_Click(object sender, RoutedEventArgs e)
        {            
            List<string> abc = new List<string>();
            //var dataRows = dataTable.Rows;
            _lasMenuForm = new LasMenuForm(dataTable);
            _lasMenuForm.Owner = this;
            _lasMenuForm.Show();            
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        async void UpdateTable(byte[] flash, Packet packet)
        {
            await Task.Run(() =>
            {
                DataTable = LoadDataTable(packet, flash);
                IsLasFile = true;
                StatusMainWindow += $"{DateTime.Now}: Загрузка завершена!\n";                
            });
            txtBoxStatus.ScrollToEnd();
        }        
        DataTable LoadDataTable(Packet packetData, byte[] flash)
        {
            DataTable myTable = new DataTable();

            myTable.Columns.Add("N");            
            foreach (var item in packetData.HeaderColumns)
            {
                string[] splitHeader = item.Split('_');
                if (splitHeader.Length == 2)
                {
                    myTable.Columns.Add(splitHeader[0]  + "\n" + splitHeader[1]);
                }
                else
                {
                    myTable.Columns.Add(item);
                }
            }            
           
            int countByteRow = packetData.LengthLine; // количество байт на строку
            byte countParams = (byte)packetData.TypeParams.Count; // количество столбцов            
            DataRow row;
            byte loadStatus = 0;
            byte tempVal;            
            int countBadBites = 0;
            int countBadTimes = 0;
            bool isBadLine = false;
            bool isBadTime = false;
            bool isZeroEndLine = false;

            if (mainPacket.endLine[0] == 0 && mainPacket.endLine[1] == 0)
            {
                isZeroEndLine = true;
            }

            for (int i = 0; i < flash.Length; i++) 
            {
                // условие захода в начало строки
                bool isGoodStartLine = flash[i] == mainPacket.ID_Packet && flash[i + 1] == mainPacket.ID_Device;

                if (i + countByteRow > flash.Length) // проверка завершенности строки, чтобы исключить выход за пределы массива байт
                {
                    if (isBadTime)
                    {
                        //ScrollStatusLasTextBox($"Ошибка данных (не удалось определить время), после строки {myTable.Rows.Count}, количество строк: {countBadTimes}");
                        StatusMainWindow += $"{DateTime.Now}: Ошибка данных (не удалось определить время), после строки {myTable.Rows.Count}, количество строк: {countBadTimes}\n";
                        isBadTime = false;
                        countBadTimes = 0;
                    }
                    countBadBites += flash.Length - i;
                    //ScrollStatusLasTextBox($"Ошибка конца файла, после строки {myTable.Rows.Count}, количество ошибочных байт: {countBadBites}");
                    StatusMainWindow += $"{DateTime.Now}: Ошибка конца файла, после строки {myTable.Rows.Count}, количество ошибочных байт: {countBadBites}\n";
                    break;
                }
                bool isGoodEndLine = false;
                // проверка двух байт на конец строки
                if (isZeroEndLine)// && i + countByteRow + 1 < flash.Length
                {
                    isGoodEndLine = true;
                    //isGoodEndLine = flash[i + countByteRow] == mainPacket.ID_Packet && flash[i + countByteRow + 1] == mainPacket.ID_Device;
                }
                else if (!isZeroEndLine)
                {
                    isGoodEndLine = flash[i + countByteRow - 2] == packetData.endLine[0] && flash[i + countByteRow - 1] == packetData.endLine[1];
                }
                
                try
                {
                    if (isGoodStartLine && isGoodEndLine) // проверка совпадения на начало и конец строки
                    {
                        if (isBadLine)
                        {
                            //ScrollStatusLasTextBox($"Ошибка после {myTable.Rows.Count} строки, количество ошибочных байт: {countBadBites}");
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
                            byte countByte = packetData.LengthParams[j-1]; // определяем количество байт на параметр
                            byte[] values = new byte[countByte]; // берем необходимое количество байт                   
                            Array.Copy(flash, i, values, 0, countByte); // копируем наш кусок

                            string valueA = packetData.GetValueByType(packetData.TypeParams[j-1], values); // вычисляем значение по типу данных
                            string valueB = packetData.CalculateValueByType(packetData.TypeCalculate[j - 1], valueA, packetData.DataCalculation[j - 1], packetData.CountSign[j-1]); // вычисляем пересчет данного по типу
                            row[j] = " " + valueB + " ";                            
                            i += countByte; // смещаем курсор по общему массиву байт                            
                        }

                        if (isBadTime)
                        {
                            //ScrollStatusLasTextBox($"Ошибка данных (не удалось определить время), после строки {myTable.Rows.Count}, количество строк: {countBadTimes}");
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
        // нажатие кнопки сохранить файл
        public async void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            string path;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "excel files|*.xlsx";
            saveFileDialog.Title = "Сохранение";
            if (saveFileDialog.ShowDialog() == true)
            {
                path = saveFileDialog.FileName;               
                await FastDtToExcelAsync(path);  
            }                                    
        }
        
       
        async Task FastDtToExcelAsync(string excelFilePath)
        {
            ScrollStatusLasTextBox($"Выполняется экспорт данных в формат .xlsx");
            await Task.Run(() =>
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
                for (int i = 0; i < DataTable.Rows.Count; i++)
                {
                    for (int j = 0; j < DataTable.Columns.Count; j++)
                    {
                        arrayDT[i + 1, j] = DataTable.Rows[i][j].ToString();
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
                // check file path
                if (!string.IsNullOrEmpty(excelFilePath))
                {
                    try
                    {
                        workSheet.SaveAs(excelFilePath);                        
                        MessageBox.Show("Экспорт данных в формат .xlsx завершен");
                        excelApp.Quit();
                    }
                    catch (Exception ex)
                    {
                        Percent = 0;
                        StatusMainWindow += $"{DateTime.Now}: {ex.Message} \n";
                    }
                }
                else
                { // no file path is given
                    excelApp.Visible = true;
                }
                Percent = 0;
            });
            ScrollStatusLasTextBox($"Экспорт данных в формат .xlsx завершен!");
        }   

        async void FastExportToTxtAsync(string path)
        {
            ScrollStatusLasTextBox("Выполняется экспорт данных в формат .txt");
            //await Task.Delay(0);

            await Task.Run(() =>
            {
                FastExportToTxt(path);
                //MessageBox.Show("Экспорт данных завершен");
                //StatusMainWindow += $"{DateTime.Now}: Экспорт данных в формат .txt завершен!\n";
            });
            ScrollStatusLasTextBox("Экспорт данных в формат .txt завершен!");
            //txtBoxStatus.ScrollToEnd();



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
        //public void ExportToExcel(string excelFilePath = null)
        //{
        //    try
        //    {
        //        if (DataTable == null || DataTable.Columns.Count == 0)
        //            throw new Exception("ExportToExcel: Null or empty input table!\n");

        //        // load excel, and create a new workbook
        //        var excelApp = new Microsoft.Office.Interop.Excel.Application();
        //        excelApp.Workbooks.Add();

        //        // single worksheet
        //        Microsoft.Office.Interop.Excel._Worksheet workSheet = (_Worksheet)excelApp.ActiveSheet;

        //        // column headings
        //        for (var i = 0; i < DataTable.Columns.Count; i++)
        //        {
        //            workSheet.Cells[1, i + 1] = DataTable.Columns[i].ColumnName.ToString();
        //        }

        //        // rows
        //        for (var i = 0; i < 102; i++)//dataTable.Rows.Count; i++)
        //        {
        //            // to do: format datetime values before printing
        //            for (var j = 0; j < dataTable.Columns.Count; j++)
        //            {
        //                workSheet.Cells[i + 2, j + 1] = dataTable.Rows[i][j].ToString();
        //            }
        //        }

        //        // check file path
        //        if (!string.IsNullOrEmpty(excelFilePath))
        //        {
        //            try
        //            {
        //                workSheet.SaveAs(excelFilePath);
        //                excelApp.Quit();
        //                MessageBox.Show("Excel file saved!");
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new Exception("ExportToExcel: Excel file could not be saved! Check filepath.\n"
        //                                    + ex.Message);
        //            }
        //        }
        //        else
        //        { // no file path is given
        //            excelApp.Visible = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("ExportToExcel: \n" + ex.Message);
        //    }
        //}
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
            for (int i = 0; i < DataTable.Rows.Count; i++)
            {
                for (int j = 0; j < DataTable.Columns.Count; j++)
                {
                    arrayDT[i+1, j] = DataTable.Rows[i][j].ToString();
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
                sb.Append(DataTable.Columns[i].ColumnName.Replace("\n", "") + "\t");
            }

            sb.AppendLine();

            for (int i = 0; i < DataTable.Rows.Count; i++)
            {                
                for (int j = 0; j < DataTable.Columns.Count; j++)
                {
                    sb.Append(DataTable.Rows[i][j].ToString() + "\t");
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
                //StatusMainWindow += $"{DateTime.Now}: Выполняется экспорт данных в формат .txt\n";
                //txtBoxStatus.ScrollToEnd();
                FastExportToTxtAsync(path);                
                //txtBoxStatus.ScrollToEnd();
            }            
        }
        void LoadSeachInfo()
        {
            string pathConfig = "Configurations";            

            if (Directory.Exists(pathConfig))
            {
                var catalogConfigs = Directory.GetFiles("Configurations", "*.cfg");                
                foreach (var path in catalogConfigs)
                {
                    List<byte[][]> data = new List<byte[][]>();
                    try
                    {
                        using (var reader = new StreamReader(path))
                        {
                            while (!reader.EndOfStream)
                            {
                                var row = reader.ReadLine();
                                if (!string.IsNullOrWhiteSpace(row))
                                {
                                    if (row.Contains('@'))
                                    {
                                        var arrayParams = row.Trim('@').Split('|', StringSplitOptions.RemoveEmptyEntries);
                                        var array = arrayParams.Select(x => x.Split(" ", StringSplitOptions.RemoveEmptyEntries)).ToArray();
                                        byte[][] bytes = Array.ConvertAll(array, x => x.Select(y => byte.Parse(y)).ToArray());                                    
                                        ConfFileInfo cf = new ConfFileInfo(path, bytes[0], bytes[1], bytes[2][0]);
                                        confFilesInfo.Add(cf);                                       
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }                        
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Возникла ошибка в ходе поиска и обработки конфиг. файлов");
                        throw new Exception();
                    }
                }
                if (confFilesInfo.Count == 0)
                { 
                    MessageBox.Show("В папке Configurations отсутствуют подходящие файлы конфигурации");
                    throw new Exception();
                }
            }
            else
            {
               MessageBox.Show("Остуствует папка Configurations с файлами конфигурации.\n" +
                   "Необходимо скопировать папку и перезапустить программу!");
                throw new Exception();
            }
        }

        void ScrollStatusLasTextBox(string message)
        {
            StatusMainWindow += $"{DateTime.Now}: {message} \n";
            txtBoxStatus.ScrollToEnd();
        }

        private void btnClearMemory_Click(object sender, RoutedEventArgs e)
        {            
            MessageBox.Show("Done!");
        }

        private void btnOpenDepthFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файл глубина-время|*.txt";
            openFileDialog.Title = "Выберите файл с глубиной и временем";
            string path;
            List<(DateTime, double)> listTimeDepth = new List<(DateTime, double)>();
            
            if (openFileDialog.ShowDialog() == true)
            {                
                path = openFileDialog.FileName;
                ScrollStatusLasTextBox($"Загрузка файла началась: {openFileDialog.SafeFileName}");
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (var reader = new StreamReader(path, Encoding.GetEncoding(1251)))
                    {
                        while (!reader.EndOfStream)
                        {
                            var row = reader.ReadLine();
                            if (!String.IsNullOrEmpty(row))
                            {                                
                                var splitLine = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                bool isTime = DateTime.TryParse(splitLine[0] + " " + splitLine[1], out DateTime time);
                                bool isDepth = double.TryParse(splitLine[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double depth);
                                if (isTime && isDepth)
                                    listTimeDepth.Add((time, depth));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ScrollStatusLasTextBox($"{ex.Message}");
                }
            }

            // ищем номер столбца с датой
            int numDate = -1;
            if (listTimeDepth.Count > 1)
            {                
                for (int i = 0; i < datagrid1.Columns.Count; i++)
                {
                    if (datagrid1.Columns[i].Header.ToString() == "[Время/Дата]")
                    {
                        numDate = i;
                    }
                }
            }

            if (numDate != -1)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(2);                
                double[] DepthArray = new double[datagrid1.Items.Count];
                int listPos = 0;
                for (int k = 40000; k < datagrid1.Items.Count; k ++)
                {
                    DataRowView row = (DataRowView)datagrid1.Items[k];
                    string text = row.Row.ItemArray[numDate].ToString();
                    if (DateTime.TryParse(text, out DateTime time1))
                    {
                        // проверка вхождения в диапазон
                        if (time1 >= listTimeDepth[0].Item1 && time1 <= listTimeDepth[listTimeDepth.Count - 1].Item1)
                        {
                            for (int i = listPos; i < listTimeDepth.Count; i++)
                            {
                                var time2 = listTimeDepth[i].Item1;
                                if (time1 > time2 - timeSpan && time1 <= time2 + timeSpan)
                                {
                                    listPos = i + 1;
                                    DepthArray[k] = listTimeDepth[i].Item2;
                                    break;
                                }
                                //else if (time1 > time2 + TimeSpan.FromSeconds(3))
                                //{
                                //    DepthArray[k] = 0;
                                //    break;
                                //}
                            }
                        }                        
                    }
                }
                
                DataGridTextColumn depthColumn = new DataGridTextColumn();                
                depthColumn.Header = "Глубина";
                Binding binding = new Binding("Depth")
                {
                    Source = Depth
                };

               
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                depthColumn.Binding = binding;

                Depth = DepthArray.ToList();
                datagrid1.Columns.Add(depthColumn);
            }
            else
            {
                ScrollStatusLasTextBox($"Не удалось обнаружить столбец [Время/Дата]");
            }
            
        }
    }
}
