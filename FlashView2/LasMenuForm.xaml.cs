using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for LasMenuForm.xaml
    /// </summary>
    public partial class LasMenuForm : Window, INotifyPropertyChanged
    {
        public string StartTimeRead { get; set; } // Стартовое время считывания
        public string EndTimeRead { get; set; } // Конечное время считывания
        public string DiamOfTrub { get; set; } // диаметр трубы
        public bool isLineCalc { get; set; } // тип расчета Кп (линейный или квардратичный)
        public List<string[]> FileDepthAndTime { get; set; } // файл с данными по глубине и времени
        public List<string[]> FileColibr { get; set; } // файл с колибровочными данными        
        double[] Coef { get; set; } // коэффициенты для расчета Кп
        DataRowCollection DataRowAVM { get; set; }
        Dictionary<double, List<string>> DepthTimeDetail;

        string fileName;
        public string FileName 
        { 
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            } 
        }
        DataTable dataTable;
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

        public LasMenuForm(DataRowCollection dataRows)
        {
            DataRowAVM = dataRows;
            FileName = "File name";
            isLineCalc = true;
            StartTimeRead = DateTime.Now.ToString();
            EndTimeRead = DateTime.Now.AddHours(2).ToString();
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {           
            string typeOfCalc;
            string diamTruba = "труба " + lb1_truba.Text;            

            if (isLineCalc)
            {
                typeOfCalc = "линейная зависимость";
                Coef = new double[2];
            }
            else
            {
                typeOfCalc = "квадратичная зависимость";
                Coef = new double[3];
            }

            for (int i = 0; i < FileColibr.Count; i++)
            {

                if (FileColibr[i][1].Contains(diamTruba) && FileColibr[i][1].Contains(typeOfCalc))
                {
                    string[] values = FileColibr[i][0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length == Coef.Length)
                    {
                        for (int j = 0; j < values.Length; j++)
                        {
                            try
                            {
                                Coef[j] = double.Parse(values[j], CultureInfo.GetCultureInfo("en-US"));
                            }
                            catch
                            {
                                MessageBox.Show("Неудалось привести данные коэффициентов к нужному типу");
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Несоответствие количества данных коэффициентов");
                        break;
                    }
                    break;
                }
            }
            UpdateDepthDate();
            //this.DialogResult = true;
            FindNearestTimeValue();
        }

        void UpdateDepthDate()
        {
            DepthTimeDetail = new Dictionary<double, List<string>>();
            int posTime = -1;
            int posDepth = -1;
            
            for (int i = 0; i < FileDepthAndTime[0].Length; i++)
            {
                if (FileDepthAndTime[0][i].Trim() == "Забой")
                    posDepth = i;
                if (FileDepthAndTime[0][i].Trim() == "Дата")
                    posTime = i;
            }

            if (posTime != -1 && posDepth != -1)
            {
                var depthStart = double.Parse(FileDepthAndTime[1][posDepth]);
                var timeStart = FileDepthAndTime[1][posTime];
                DepthTimeDetail.Add(depthStart, new List<string>() {timeStart});
                for (int i = 2; i < FileDepthAndTime.Count; i++)
                {                    
                    for (int j = i; j < FileDepthAndTime.Count; j++)
                    {
                        var depthStop = int.Parse(FileDepthAndTime[j][posDepth]);                       
                        var timeStop = FileDepthAndTime[j][posTime];

                        if (depthStop > depthStart && depthStop - depthStart <= 10)
                        {
                            double deltaDepth = depthStop - depthStart;                            
                            DateTime time2 = DateTime.Parse(timeStop);
                            DateTime time1 = DateTime.Parse(timeStart);
                            TimeSpan deltaTime = time2 - time1;
                            double timeStep = 1.0 / (deltaDepth * 10);
                            double depthStep = 0.1;
                            for (double k = timeStep; k <= 1; k += timeStep)
                            {
                                double newDepth = depthStart + depthStep;
                                depthStep = Math.Round(depthStep += 0.1, 1);                                
                                string newTime = (time1 + (deltaTime * k)).ToString();
                              
                                if (!DepthTimeDetail.TryAdd(newDepth, new List<string>() {newTime})) 
                                {
                                    DepthTimeDetail[newDepth].Add(newTime);
                                }                                
                            }                            
                            depthStart = depthStop;
                            timeStart = timeStop;                            
                        } 
                        else if (depthStop < depthStart && depthStart - depthStop <= 10)
                        {
                            //if (depthStop == 1752)
                            //{

                            //}
                            double deltaDepth = depthStart - depthStop;                            
                            DateTime time2 = DateTime.Parse(timeStop);
                            DateTime time1 = DateTime.Parse(timeStart);
                            TimeSpan deltaTime = time2 - time1;

                            double timeStep = 1.0 / (deltaDepth * 10);
                            double depthStep = 0.1;
                            for (double k = timeStep; k <= 1; k += timeStep)
                            {
                                double newDepth = depthStart - depthStep;
                                depthStep = Math.Round(depthStep += 0.1, 1);
                                string newTime = (time1 + (deltaTime * k)).ToString();
                                if (!DepthTimeDetail.TryAdd(newDepth, new List<string>() { newTime }))
                                {
                                    DepthTimeDetail[newDepth].Add(newTime);
                                }
                            }
                            depthStart = depthStop;
                            timeStart = timeStop;
                        }
                        else if (depthStart != depthStop)
                        {
                            if (!DepthTimeDetail.TryAdd(depthStop, new List<string>() { timeStop }))
                            {
                                DepthTimeDetail[depthStop].Add(timeStop);
                            }
                            depthStart = depthStop;
                            timeStart = timeStop;
                        }                        
                        i++;
                    }

                }                
            }         
        }
        void FindNearestTimeValue()
        {
            // Надо переделать изначальное хранилище данных с глубиной по ключу. Сделать ключом время
            SortedDictionary<DateTime, double> dicr = new SortedDictionary<DateTime, double>();            
            var list = DepthTimeDetail.ToList();
            for (int i1 = 0; i1 < list.Count; i1++)
            {
                for (int j1 = 0; j1 < list[i1].Value.Count; j1++)
                {
                    dicr.Add(DateTime.Parse(list[i1].Value[j1]), list[i1].Key);
                }
            }
            var listTimeKey = dicr.ToList();

            DateTime timeStartMetr = listTimeKey[0].Key;
            double depthStartMetr = listTimeKey[0].Value;

            DateTime timeEndStartMetr = new DateTime();

            for (int i = 0; i < listTimeKey.Count; i++)
            {
                double depthEndMetr = listTimeKey[i].Value;
                
                if (depthStartMetr - depthEndMetr == 1)
                {                    
                    timeEndStartMetr = listTimeKey[i].Key;
                }               
                else if (Math.Abs(depthStartMetr - depthEndMetr) > 1)  // надо доработать логику если отличие более метра
                {
                    depthStartMetr = depthEndMetr;
                    timeStartMetr = listTimeKey[i].Key;
                }

                int startPosTimeMetr = -1;
                int endPosTimeMetr = -1;
                for (int j = 0; j < DataRowAVM.Count; j++)
                {
                    DataRow row = DataRowAVM[j];
                    var timeValue = row["[Время/Дата]"].ToString(); // время во флешке
                    DateTime timeValueRow = new DateTime();
                    if (timeValue != null)
                    {
                        timeValueRow = DateTime.Parse(timeValue);
                        if (timeValueRow >= timeStartMetr && timeValueRow - timeStartMetr <= TimeSpan.FromSeconds(60))
                        {

                        }
                        //var ts = timeValueRow - timeStartMetr;
                        //if (ts)
                    }
                    
                    
                    //var mz = row["[ННК1/ННК1(вода)]"].ToString();
                    //var bz = row["[ННК2/ННК2(вода)]"].ToString();
                    //foreach (DataRow row in DataRowAVM)
                    //{
                    //    var timeValueRow = row["[Время/Дата]"].ToString();
                    //    var mz = row["[ННК1/ННК1(вода)]"].ToString();
                    //    var bz = row["[ННК2/ННК2(вода)]"].ToString();
                    //    if (timeValueRow != null)
                    //    {
                    //        DateTime timeFlash = DateTime.Parse(timeValueRow);
                    //    }
                    //}
                }

            }


            for (int i = 0; i - 1 < DepthTimeDetail.Count; i++)
            {
                var timeList1 = list[i].Value;
                var depth1 = list[i].Key;
                var timeList2 = list[i + 1].Value;
                var depth2 = list[i + 1].Key;
                if (depth1 - depth2 == 0.1)
                {
                    for (int z = 0; z < timeList1.Count; z++)
                    {
                        DateTime t1 = DateTime.Parse(timeList1[z]);
                        DateTime t2 = DateTime.Parse(timeList2[z]);
                    }
                }
                

                for (int j = 1; j < DataRowAVM.Count; j++)
                {
                    foreach (DataRow row in DataRowAVM)
                    {
                        var timeValueRow = row["[Время/Дата]"].ToString();
                        var mz = row["[ННК1/ННК1(вода)]"].ToString();
                        var bz = row["[ННК2/ННК2(вода)]"].ToString();
                        if (timeValueRow != null)
                        {
                            DateTime timeFlash = DateTime.Parse(timeValueRow);
                        }                        
                    }
                }
            }
        }
        private void btn_LoadDepthAndTime_Click(object sender, RoutedEventArgs e)
        {           
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string path;
            bool isTable = false;
            int countHeaders = 0;
            int posDate = 0;            
            int year = 0;
            int month = 0;
            // выводим данные из файла на экран
            if (openFileDialog.ShowDialog()==true)
            {
                FileDepthAndTime = new List<string[]>();
                path = openFileDialog.FileName;
                FileName = openFileDialog.SafeFileName;
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);                   
                    using (var reader = new StreamReader(path, Encoding.GetEncoding(1251)))
                    {
                        while (!reader.EndOfStream)
                        {
                            var row = reader.ReadLine();
                            if (!string.IsNullOrEmpty(row))
                            {
                                // ищем год по выражению
                                if (row.ToLower().Contains("данные с"))
                                {
                                    string[] arrayDate = row.Split();
                                    foreach (string date in arrayDate)
                                    {
                                        if (DateTime.TryParse(date, out DateTime result))
                                        {
                                            year = result.Year;
                                            month = result.Month;
                                            break;
                                        }                                      
                                    }
                                }
                                // ищем начало названия столбцов
                                if (row.Contains('|') && row.Contains("Забой"))
                                {
                                    isTable = true;
                                    var tempArray = row.Split('|', StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < tempArray.Length; i++)
                                    {
                                        if (tempArray[i].ToLower().Trim() == "дата" && tempArray[i-1].ToLower().Trim() == "время")
                                        {
                                            posDate = i;
                                        }
                                    }
                                    countHeaders = tempArray.Length;
                                    tempArray[posDate - 1] = "Дата";
                                    FileDepthAndTime.Add(tempArray.SkipLast(1).ToArray());
                                }                                
                                // записываем табличные данные
                                if (isTable)
                                {
                                    string[] array = row.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).ToArray(); // массив с данными
                                    // проверка начала численных данных
                                    bool isTableData = int.TryParse(array[0], out int temRes); 
                                    if (isTableData)
                                    {
                                        var dayAndMOnth = array[posDate].Split('.');
                                        if (int.TryParse(dayAndMOnth[1], out int resultMonth) && resultMonth < month)
                                        {
                                            month = resultMonth;
                                            year++;
                                        }
                                     
                                        array[posDate-1] = $"{array[array.Length - 2]}:00 {array[array.Length - 1]}.{year}";                                        
                                        if (array.Length == countHeaders)
                                        {
                                            FileDepthAndTime.Add(array.SkipLast(1).ToArray());
                                        }
                                    }                
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                DataTable dt = new DataTable();
                for (int i = 0; i < FileDepthAndTime[0].Length; i++)
                {
                    DataColumn dataColumn = new DataColumn();                    
                    dataColumn.ColumnName = FileDepthAndTime[0][i].Trim();                    
                    dt.Columns.Add(dataColumn);
                }
                
                if (FileDepthAndTime.Count>2)
                {                    
                    for(int i = 1; i < FileDepthAndTime.Count; i++)
                    {
                        var rowTable = dt.NewRow();
                        for (int j = 0; j < FileDepthAndTime[i].Length; j++)
                        {
                            rowTable[j] = FileDepthAndTime[i][j];
                        }
                        dt.Rows.Add(rowTable);
                    }
                }
                else
                {
                    MessageBox.Show("Файл не содержит достаточное количество данных");
                }

                DataTable = dt;
                dtg_DepthAndTime.HorizontalAlignment = HorizontalAlignment.Center;                            
            }
            // считываем данные из калибровочного файла
            try
            {
                FileColibr = new List<string[]>();
                using (var reader = new StreamReader(@"Calibrations\NNK_10_25.08.2022.nk", Encoding.GetEncoding(1251)))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            var splitLine = line.Split(':');
                            if (splitLine.Length > 1)
                            {
                                FileColibr.Add(splitLine);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void r2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.Contains('[') || e.PropertyName.Contains(' ') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            }
        }

        //private void btn_LookColibFile_Click(object sender, RoutedEventArgs e)
        //{
        //    StringBuilder coefForShow = new StringBuilder();
        //    for (int i = 0; i < coef.Length; i++)
        //    {
        //        coefForShow.AppendLine($"{i + 1} коэфц.: {coef[i]}");
        //    }
        //    MessageBox.Show(coefForShow.ToString());
        //}
    }
}
