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
using FlashZTK_I;
using FlashZTK_I.Model;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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

        //public List<List<(DateTime, double)>> Depth;
        //public List<DepthTimeFile> DepthData;

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

        public MainWindow()
        {
            InitializeComponent();           
            DataContext = this;
            IsLasFile = false;

            MessageBox.Show("Данная версия программы поддерживает только один тип приборов : ННГК");
        }       

        // Обработка конф данных
        void HandleConfigData(List<List<string>> dataConfig, Packet mainPacket)
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

        async void UpdateTable(byte[] flash, Packet packet, List<DepthTimeFile>? DepthData)
        {
            await Task.Run(() =>
            {
                DataTable = LoadDataTable(packet, flash, DepthData);
                IsLasFile = true;
                StatusMainWindow += $"{DateTime.Now}: Загрузка завершена!\n";                
            });            
        }        
        DataTable LoadDataTable(Packet mainPacket, byte[] flash, List<DepthTimeFile> Depth = null)
        {
            DataTable myTable = new DataTable();
            DataRow row;
            bool isAddCols = Depth != null;

            int countByteRow = mainPacket.LengthLine; // количество байт на строку
            byte countParams = (byte)mainPacket.TypeParams.Count; // количество столбцов            
            byte loadStatus = 0;
            byte tempVal;
            int countBadBites = 0;
            int countBadTimes = 0;
            bool isBadLine = false;
            bool isBadTime = false;
            bool isZeroEndLine = false;
            int listPos = 0; // позиция в коллекции файла глубина - время
            DepthTimeFile? saveDTF = null;
            myTable.Columns.Add("N");            
            foreach (var item in mainPacket.HeaderColumns)
            {
                string[] splitHeader = item.Split('_');
                if (splitHeader.Length == 2)
                {
                    myTable.Columns.Add(splitHeader[0]  + "\n" + splitHeader[1], typeof(string));
                }
                else
                {
                    myTable.Columns.Add(item);
                }
            }         
                        
            // Добавление дополнительного столбца если мы указали путь к глубинным файлам
            if (isAddCols)
            {
                myTable.Columns.Add("[Глубина]");
                bool isAddSecCol = Depth.Select(x=>x.StatusList).Any(y => y.Count > 0);
                mainPacket.handlerFillDepthCol += AddDepthValue;

                if (isAddSecCol)
                    myTable.Columns.Add("[Состояние]");
            }               
            
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
                        StatusMainWindow += $"{DateTime.Now}: Ошибка данных (не удалось определить время), после строки {myTable.Rows.Count}, количество строк: {countBadTimes}\n";
                        isBadTime = false;
                        countBadTimes = 0;
                    }
                    countBadBites += flash.Length - i;                    
                    StatusMainWindow += $"{DateTime.Now}: Ошибка конца файла, после строки {myTable.Rows.Count}, количество ошибочных байт: {countBadBites}\n";
                    break;
                }
                bool isGoodEndLine = false;
                // проверка двух байт на конец строки
                if (isZeroEndLine)// && i + countByteRow + 1 < flash.Length
                {
                    isGoodEndLine = true;                    
                }
                else if (!isZeroEndLine)
                {
                    isGoodEndLine = flash[i + countByteRow - 2] == mainPacket.endLine[0] && flash[i + countByteRow - 1] == mainPacket.endLine[1];
                }
                
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
                            byte countByte = mainPacket.LengthParams[j-1]; // определяем количество байт на параметр
                            byte[] values = new byte[countByte]; // берем необходимое количество байт                   
                            Array.Copy(flash, i, values, 0, countByte); // копируем наш кусок

                            string valueA = mainPacket.GetValueByType(mainPacket.TypeParams[j-1], values, row); // вычисляем значение по типу данных
                            string valueB = mainPacket.CalculateValueByType(mainPacket.TypeCalculate[j - 1], valueA, mainPacket.DataCalculation[j - 1], mainPacket.CountSign[j-1]); // вычисляем пересчет данного по типу
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

            // Лок. метод сравнение дат файла флеш и глубина время
            void AddDepthValue(string data, DataRow row)
            {
                DateTime dtFlash = DateTime.Parse(data);
                TimeSpan timeSpan = TimeSpan.FromSeconds(1);

                // находим актуальный лист с данными
                if (Depth.Count > 0)
                {
                    if (saveDTF == null)
                    {
                        foreach (var item in Depth)
                        {
                            if (dtFlash >= item.DateList[0] && dtFlash <= item.DateList[item.DateList.Count - 1])
                            {
                                saveDTF = item;
                                break;
                            }
                        }
                        // если не нашли
                        if (saveDTF == null)
                        {
                            row[countParams + 1] = -999;
                            row[countParams + 2] = 0;
                            return;
                        }
                    }

                    // проходимся по нему
                    if (dtFlash >= saveDTF.DateList[0] && dtFlash <= saveDTF.DateList[saveDTF.DateList.Count - 1])
                    {
                        for (int i = listPos; i < saveDTF.DateList.Count; i++)
                        {
                            var dtDateDepth = saveDTF.DateList[i];
                            if (dtFlash >= dtDateDepth - timeSpan && dtFlash <= dtDateDepth + timeSpan)
                            {
                                listPos = i + 1;
                                row[countParams + 1] = saveDTF.DepthList[i];
                                row[countParams + 2] = saveDTF.StatusList[i] ? 1 : 0;
                                return;
                            }
                            else if(dtFlash < dtDateDepth)
                            {
                                row[countParams + 1] = -999;
                                row[countParams + 2] = 0;
                                return;
                            }

                            if (i == saveDTF.DateList.Count)
                            {
                                Depth.Remove(saveDTF);
                                saveDTF = null;
                                listPos = 0;
                            }
                        }
                    }
                    else
                    {
                        Depth.Remove(saveDTF);
                        saveDTF = null;
                        listPos = 0;
                        AddDepthValue(data, row);
                    }
                }
                else
                {
                    row[countParams + 1] = -999;
                    row[countParams + 2] = 0;
                }


                // подумать над возможной оптимизации процесса поиска подходящих дат
                //if (Depth.Count > 0)
                //{
                //    for (int j = 0; j < Depth.Count; j++)
                //    {
                //        var dtf = Depth[j];
                //        if (dtFlash >= dtf.DateList[0] && dtFlash <= dtf.DateList[dtf.DateList.Count - 1])
                //        {
                //            for (int i = listPos; i < dtf.DateList.Count; i++)
                //            {
                //                var dtDateDepth = dtf.DateList[i];
                //                if (dtFlash >= dtDateDepth - timeSpan && dtFlash <= dtDateDepth + timeSpan)
                //                {
                //                    listPos = i + 1;
                //                    row[countParams + 1] = dtf.DepthList[i];
                //                    row[countParams + 2] = dtf.StatusList[i] ? 1 : 0;
                //                    return;
                //                }                                                       
                //            }
                //        }
                //        else
                //        {
                //            row[countParams + 1] = -999;
                //            row[countParams + 2] = 0;
                //            listPos = 0;
                //            continue;
                //        }
                //    }
                //}
                //else
                //{
                //    row[countParams + 1] = -999;
                //    row[countParams + 2] = 0;
                //}
            }
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
            ScrollStatusTextBox($"Выполняется экспорт данных в формат .xlsx");
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
            ScrollStatusTextBox($"Экспорт данных в формат .xlsx завершен!");
        }   

        async void FastExportToTxtAsync(string path)
        {
            ScrollStatusTextBox("Выполняется экспорт данных в формат .txt");
            //await Task.Delay(0);

            await Task.Run(() =>
            {
                FastExportToTxt(path);
                //MessageBox.Show("Экспорт данных завершен");
                //StatusMainWindow += $"{DateTime.Now}: Экспорт данных в формат .txt завершен!\n";
            });
            ScrollStatusTextBox("Экспорт данных в формат .txt завершен!");
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
        List<ConfFileInfo> GetConfigData()
        {
            List<ConfFileInfo> confFilesInfo = new List<ConfFileInfo>();
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
                        throw new Exception("Возникла ошибка в ходе поиска и обработки конфиг. файлов");
                    }
                }
                if (confFilesInfo.Count == 0)
                { 
                    MessageBox.Show("В папке Configurations отсутствуют подходящие файлы конфигурации");
                    throw new Exception("В папке Configurations отсутствуют подходящие файлы конфигурации");
                }
            }
            else
            {
               MessageBox.Show("Остуствует папка Configurations с файлами конфигурации.\n" +
                   "Необходимо скопировать папку и перезапустить программу!");
                throw new Exception("Остуствует папка Configurations с файлами конфигурации.");
            }

            return confFilesInfo;
        }

        void ScrollStatusTextBox(string message)
        {
            StatusMainWindow += $"{DateTime.Now}: {message} \n";
            txtBoxStatus.ScrollToEnd();
        }

        // Чтение данных из выбранных файлов с глубиной и временем в переменную Depth
        List<DepthTimeFile> OpenDepthFiles(List<string> depthPaths)
        {
            //List<List<(DateTime, double)>> resultList = new List<List<(DateTime, double)>>();            
            List<DepthTimeFile> resultList = new List<DepthTimeFile>();
            try
            {                
                foreach (string path in depthPaths)
                {
                    DepthTimeFile dtf = new DepthTimeFile();
                    //List<(DateTime, double)> listTimeDepth = new List<(DateTime, double)>();
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (var reader = new StreamReader(path, Encoding.GetEncoding(1251)))
                    {
                        bool isData = false;                        
                                                                   
                        while (!reader.EndOfStream)
                        {
                            var row = reader.ReadLine();
                            if (!String.IsNullOrEmpty(row))
                            {       
                                if (!isData)
                                {

                                    if (row.ToLower().Contains("данные с"))
                                    {
                                        var splitLine = row.Split(" ");
                                        foreach (var item in splitLine)
                                        {
                                            if(DateTime.TryParse(item, out DateTime data))
                                            {
                                                dtf.Year += "." + data.Year.ToString();
                                                break;
                                            }
                                        }
                                        continue;
                                    }

                                    if (row.ToLower().Contains("дата") && row.ToLower().Contains("время") && row.ToLower().Contains("забой"))
                                    {   
                                        if (!string.IsNullOrEmpty(dtf.Year))
                                        {
                                            dtf.Separator = '|';
                                            dtf.DepthName = "забой"; // версия файла без глубиномера
                                            dtf.ColNumbers[3] = 999; // т.к. в старой версии файла таких данных уже не будет
                                        }

                                        var splitLine = row.Split(dtf.Separator, StringSplitOptions.RemoveEmptyEntries);
                                        dtf.ColNumbers[0] = Array.FindIndex(splitLine, value => value.ToLower().Trim() == dtf.TimeName);
                                        dtf.ColNumbers[1] = Array.FindIndex(splitLine, value => value.ToLower().Trim() == dtf.DateName);
                                        dtf.ColNumbers[2] = Array.FindIndex(splitLine, value => value.ToLower().Trim() == dtf.DepthName);
                                        dtf.ColNumbers[3] = Array.FindLastIndex(splitLine, value => value.Trim() == dtf.StatusName) - 1; // из-за лишнего пробела

                                        if (dtf.ColNumbers.Any(x => x == -1))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            // вычисляем длину строки, для новой версии файла она будет меньше на 2 столбца
                                            // из-за sbSendData акт.  sbSendData сост.
                                            dtf.lengthStr = dtf.Separator == ' ' ? splitLine.Length - 2 : splitLine.Length;                                            
                                            isData = true;
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    var splitLine = row.Split(dtf.Separator, StringSplitOptions.RemoveEmptyEntries);
                                    if (dtf.lengthStr != splitLine.Length)
                                    {
                                        continue;
                                    }

                                    bool isTime = DateTime.TryParse(splitLine[dtf.ColNumbers[1]] + dtf.Year + " " + splitLine[dtf.ColNumbers[0]], out DateTime time);
                                    bool isDepth = double.TryParse(splitLine[dtf.ColNumbers[2]], NumberStyles.Any, CultureInfo.InvariantCulture, out double depth);
                                    bool status = false;
                                    if (dtf.ColNumbers[3] != 999)
                                    {
                                        string statStr = splitLine[dtf.ColNumbers[3]];
                                        status = statStr == "отж." ? false : true;
                                    }                                        

                                    if (isTime && isDepth)
                                    {
                                        if (dtf.PrevDate != null)
                                        {
                                            if (dtf.PrevDate > time)
                                            {
                                                time = time.AddYears(1);
                                                bool isParsedValue = int.TryParse(dtf.Year.TrimStart('.'), out int val);
                                                val++;
                                                if (isParsedValue)
                                                {
                                                    dtf.Year = "." + val + ToString();
                                                }                                               
                                                dtf.PrevDate = time;
                                            }
                                            else
                                            {
                                                dtf.PrevDate = time;
                                            }
                                        }
                                        else
                                        {
                                            dtf.PrevDate = time;
                                        }
                                        dtf.DepthList.Add(depth);
                                        dtf.DateList.Add(time);
                                        dtf.StatusList.Add(status);                                       
                                    }
                                        
                                }                               
                            }
                        }
                    }
                    if (dtf.DepthList.Count > 0)
                    {
                        ScrollStatusTextBox($"{path} данные считаны успешно");                        
                        resultList.Add(dtf);
                    }                    
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return resultList;           
        }

        private void btnOpenFileFlash_Click(object sender, RoutedEventArgs e)
        {
            bool isDepth;
            var newFlashWindow = new OpenDataFilesDialog();
            byte[]? FlashFile;
            Packet? mainPacket;
            List<List<string>>? dataConfig;
            List<DepthTimeFile>? DepthData = null;
            newFlashWindow.ShowDialog();
            if (newFlashWindow.FlashPath != null)
            {
                isDepth = newFlashWindow.DepthPath != null;
                if (isDepth)
                {
                    try
                    {
                        DepthData = OpenDepthFiles(newFlashWindow.DepthPath.ToList());                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }

                try
                {
                    FlashFile = File.ReadAllBytes(newFlashWindow.FlashPath).Skip(384).ToArray();
                    ScrollStatusTextBox($"{newFlashWindow.FlashPath} данные flash считаны успешно");
                    mainPacket = new Packet();
                    dataConfig = FillDateConf(FlashFile, mainPacket);
                    HandleConfigData(dataConfig, mainPacket);
                    Percent = 0;
                    UpdateTable(FlashFile, mainPacket, DepthData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }                               
            }                        
        }

        List<List<string>> FillDateConf(byte[]? FlashFile, Packet mainPacket)
        {                     
            List<List<string>> dataConfig = new List<List<string>>();           

            // Получение данных с конфигурационных файлов
            List<ConfFileInfo> confFilesInfo = GetConfigData();

            try
            {
                // считываем данные флеш-файла, запускаем процесс поиска подходящих конфиг. данных                         
                string pathConfig = "";
                for (int i = 0; i < FlashFile.Length; i++)
                {
                    foreach (var cf in confFilesInfo)
                    {
                        if (i + cf.LengthLine + 1 < FlashFile.Length)
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
                                pathConfig = cf.PathFile;
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
                    ScrollStatusTextBox("Возникла ошибка: подходящий конфигурационный файл не найден");
                    throw new Exception("Возникла ошибка: подходящий конфигурационный файл не найден");
                }

                if (!string.IsNullOrEmpty(pathConfig))
                {
                    //считываем данные конфиг - файла

                    char[] separators = { ' ', '\t' };
                    using (var reader = new StreamReader(pathConfig))
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

                return dataConfig;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Возникла ошибка: {ex.Message}");
                throw;
            }           
        }
    }
}
