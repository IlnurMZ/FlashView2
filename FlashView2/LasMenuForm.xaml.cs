using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        private string diamTruba;
        public string DiamTruba
        {
            get { return diamTruba; }
            set 
            { 
                diamTruba = value;
                //OnPropertyChanged("DiamTruba");
            }
        }


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

        public LasMenuForm()
        {
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
            DiamOfTrub = lb1_truba.Text;
            isLineCalc = rb1_Lin.IsChecked.Value;
            this.DialogResult = true;
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
                                if (row.Contains('|') && row.Contains("Забой")) // ищем начало названия столбцов
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
                                }

                                if (row.ToLower().Contains("данные с") ) // ищем год по выражению
                                {
                                    string[] arrayDate = row.Split();
                                    foreach(string date in arrayDate)
                                    {
                                        if (DateTime.TryParse(date, out DateTime result))
                                        {
                                            year = result.Year;
                                            month = result.Month;
                                            break;
                                        }                                        
                                    }
                                }

                                if (isTable)
                                {
                                    string[] array = row.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).ToArray(); // массив с данными
                                    
                                    bool isTableData = int.TryParse(array[0], out int temRes);
                                    if (isTableData)
                                    {
                                        var dayAndMOnth = array[posDate].Split('.');
                                        if (int.TryParse(dayAndMOnth[1], out int resultMonth) && resultMonth < month)
                                        {
                                            month = resultMonth;
                                            year++;
                                        }                                        
                                        array[posDate] = $"{array[array.Length - 2]}:00 {array[array.Length - 1]}.{year}";                                      
                                    }
                                    
                                    if (array.Length == countHeaders)
                                    {
                                        FileDepthAndTime.Add(array);
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
                    for(int i = 2; i < FileDepthAndTime.Count; i++)
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

                dt.Columns.Remove("Время");
                DataTable = dt;
                dtg_DepthAndTime.HorizontalAlignment = HorizontalAlignment.Center;

                try
                {
                    FileColibr = new();
                    using (var reader = new StreamReader(@"Calibrations\NNK_10_25.08.2022.nk", Encoding.GetEncoding(1251)))
                    {
                        while(!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                var splitLine = line.Split();
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

                DiamTruba = "труба " + lb1_truba.Text;
                for (int i = 0; i < FileColibr.Count; i++)
                {
                    if (FileColibr[i].Contains(DiamTruba) && FileColibr[i].Contains("линейная зависимость")) 
                    {
                        MessageBox.Show("Yes");
                    }
                }
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

        private void btn_LookColibFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hi");
        }
    }
}
