using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using FlashView2.Model;
using Microsoft.Win32;
using static System.Environment;
using static System.IO.Path;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for LasMenuForm.xaml
    /// </summary>
    public partial class LasMenuForm : Window, INotifyPropertyChanged
    {       
        public bool isLineCalc { get; set; } // тип расчета Кп (линейный или квардратичный)                               
        public bool IsSetInterval { get; set; } // проверка включения радиобатона "В задаваемом интервале"        
        DateTime startDateTime;
        public DateTime StartTimeRead
        {
            get
            {
                return startDateTime;
            }
            set
            {
                startDateTime = value;
                OnPropertyChanged("StartTimeRead");
            }
        } // Стартовое времея считывания
        DateTime endTimeRead;
        public DateTime EndTimeRead
        {
            get
            {
                return endTimeRead;
            }
            set
            {
                endTimeRead = value;
                OnPropertyChanged("EndTimeRead");
            }
        } // Конечное время считывания

        bool isUseStat;
        public bool IsUseStat
        {
            get
            {
                return isUseStat;
            }
            set
            {
                isUseStat = value;
                OnPropertyChanged("IsUseStat");
            }
        }// проверка включения радиобатона "Вперед" в группе "Сдвигать время"

        bool isMoveTime;
        public bool IsMoveTime
        {
            get
            {
                return isMoveTime;
            }
            set
            {
                isMoveTime = value;
                OnPropertyChanged("IsMoveTime");
            }
        } // доступ к кнопке включения "Применить" в группе "Сдвигать время"
        bool isMoveTimeUp; 
        public bool IsMoveTimeUp
        {
            get
            {
                return isMoveTimeUp;
            }
            set
            {
                isMoveTimeUp = value;
                OnPropertyChanged("IsMoveTimeUp");
            }
        }// проверка включения радиобатона "Вперед" в группе "Сдвигать время"

        bool isOnePoint;
        public bool IsOnePoint
        {
            get
            {
                return isOnePoint;
            }
            set
            {
                isOnePoint = value;
                OnPropertyChanged("IsOnePoint");
            }
        }// проверка включения чекбокса "Использовать осреднение точек" в группе "Сдвигать время"

        string shiftTime;
        public string ShiftTime
        {
            get 
            {
                return shiftTime; 
            }
            set 
            {
                shiftTime = value; 
                OnPropertyChanged("ShiftTime"); 
            }
        }// 00:00:00
        DepthTimeLas dtl;       
        CalibrFile calibrFile;
        public CalibrFile MyCalibrFile 
        {
            get
            {
                return calibrFile;
            }
            set
            {
                calibrFile = value;
                OnPropertyChanged("MyCalibrFile");
            } 
        }
        //double[] Coef { get; set; } // коэффициенты для расчета Кп
        DataRowCollection DataRowAVM { get; set; }        
        string statusLasMenu;
        public string StatusLasMenu 
        { 
            get
            {
                return statusLasMenu;
            }
            set
            {
                statusLasMenu = value;
                OnPropertyChanged("StatusLasMenu");
            } 
        }

        private int percentLas; // проценты загрузки для прогресбара
        public int PercentLas
        {
            get
            {
                return percentLas;
            }
            set
            {
                percentLas = value;
                OnPropertyChanged("PercentLas");
            }
        }

        private bool isOpenFile;
        public bool IsOpenFile
        {
            get
            {
                return isOpenFile;
            }
            set
            {
                isOpenFile = value;
                OnPropertyChanged("IsOpenFile");
            }
        }

        private bool isOpenCalibFile;
        public bool IsOpenCalibFile
        {
            get
            {
                return isOpenCalibFile;
            }
            set
            {
                isOpenCalibFile = value;
                OnPropertyChanged("IsOpenCalibFile");
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
        public LasMenuForm(DataTable dt)
        {
            DataRowAVM = dt.Rows;          
            isLineCalc = true;           
            InitializeComponent();
            DataContext = this;
            IsMoveTimeUp = true;
            IsMoveTime = false;
            ShiftTime = "00:00:00";
            IsOnePoint = true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        // кнопка формирования LAS
        void Button_FormLasClick(object sender, RoutedEventArgs e)
        {      
            try
            {
                ChoiseCalibrData(); // сверяем включенные опции (кнопки, выбр.пункты меню)
                                    // для выбора необходимых конф данных
                PercentLas = 5;
                if (!double.TryParse(txtBoxNULL.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double nullValue))
                {
                    nullValue = -999.99;
                    ScrollStatusLasTextBox($"Ошибка конвертации Null value. Установлено дефолтное значение {nullValue}");
                }

                if (dtl.Separator == '|')
                {                    
                    var timeDepth = UpdateDepthDate();
                    PercentLas = 20;
                    var dataLas = GetDataForLasType1(timeDepth);
                    PercentLas = 80;
                    RecordDataLas(nullValue, dataLas);
                    PercentLas = 0;
                }
                else if (dtl.Separator == ' ')
                {
                    var listDepthDate = UpdateDepthDate2();
                    PercentLas = 20;
                    var dataLas = GetDataForLasType2(listDepthDate);
                    PercentLas = 80;
                    RecordDataLas2(nullValue, dataLas);
                    PercentLas = 0;
                }
                else
                {
                    throw new Exception("Формат файла не поддерживается");
                }
               
            }
            catch (Exception ex)
            {
                MessageBox.Show("При формировании las-файла возникла ошибка");
                ScrollStatusLasTextBox(ex.Message);
            }                     
        }

        // Код для дальнейшей модификации
        private List<(double, DateTime)> UpdateDepthDate2()
        {            
            List<(double, DateTime)> listDepthTime = new List<(double, DateTime)>();

            for (int i = 0; i < dtl.ColNames.Count; i++)
            {
                if (dtl.ColNames[i].ToLower().Contains("забой"))
                {
                    dtl.ColumnZab = i;
                    break;
                }
            }

            int startPos = 0;
            int endPos = dtl.Data.Count;

            // Если включена опция в заданном интервале, корректируем начало и конец таблицы
            if (IsSetInterval)
            {
                // Проверка корректности нач. и конечю даты
                if (StartTimeRead >= EndTimeRead)
                {
                    throw new Exception("Считать данные => Стартовое время диапазона должно быть меньше конечного времени");
                }

                // Определение начальной строки таблицы
                for (int i = 0; i < dtl.Data.Count; i++)
                {
                    bool isParsDate = DateTime.TryParse(dtl.Data[i][dtl.ColumnDate], out DateTime tempDate);
                    if (isParsDate)
                    {
                        if (tempDate >= StartTimeRead)
                        {
                            bool isParsDepth = double.TryParse(dtl.Data[i][dtl.ColumnZab], NumberStyles.Any, CultureInfo.InvariantCulture, out double tempDepth);
                            if (isParsDepth)
                            {
                                startPos = i;
                                break;
                            }
                        }
                    }
                }

                // Проверка что найдена первая позиция строки по времени-старту
                if (startPos == 0)
                {
                    ScrollStatusLasTextBox("Не удалось найти позицию строки по времени начало интервала." +
                        "\nВозможно неверно указан временной интервал");
                    throw new ArgumentException("Неверно указан временной интервал");
                }

                // Определение конечной строки таблицы
                for (int i = startPos; i < dtl.Data.Count; i++)
                {
                    bool isParsDate = DateTime.TryParse(dtl.Data[i][dtl.ColumnDate], out DateTime tempDate);
                    if (isParsDate)
                    {
                        if (tempDate > EndTimeRead)
                        {
                            bool isParsDepth = double.TryParse(dtl.Data[i][dtl.ColumnZab], NumberStyles.Any, CultureInfo.InvariantCulture, out double tempDepth);
                            if (isParsDepth)
                            {
                                endPos = i - 1;
                                break;
                            }
                        }
                        else if (tempDate == EndTimeRead)
                        {
                            bool isParsDepth = double.TryParse(dtl.Data[i][dtl.ColumnZab], NumberStyles.Any, CultureInfo.InvariantCulture, out double tempDepth);
                            if (isParsDepth)
                            {
                                endPos = i;
                                break;
                            }
                        }
                    }
                }
                // Проверка что найдена последняя позиция строки по времени-старту
                //if (endPos == 1)
                //{
                //    ScrollStatusLasTextBox("Не удалось найти позицию строки по времени конца интервала." +
                //        "\nВозможно неверно указан временной интервал");
                //    throw new ArgumentException("Неверно указан временной интервал");
                //}
            }

            double depth1 = 0;
            double depth2 = 0;
            DateTime date1 = DateTime.MinValue;
            DateTime date2 = DateTime.MinValue;

            // ищем первую глубину для отключенного статуса
            if (!IsUseStat)
            {
                depth1 = double.Parse(dtl.Data[startPos][dtl.ColumnZab], CultureInfo.CurrentCulture);
                //depth1 = Math.Round(depth1, 1, MidpointRounding.ToNegativeInfinity);                               
            }
            // ищем первую глубину для включенного статуса
            else
            {
                dtl.ColumnStat = 3; // 3 - активность, 4 - нажатие
                FindFirstDepth(ref startPos, endPos, ref depth1, ref date1);
            }

            for (int i = startPos; i < endPos; i++)
            {
                // проверка активности при включенной опции
                if (IsUseStat)
                {
                    if (dtl.Data[i][dtl.ColumnStat].Trim() != "актив" || dtl.Data[i][dtl.ColumnStat + 1].Trim() != "наж")
                    {
                        startPos = i;
                        FindFirstDepth(ref startPos, endPos, ref depth1, ref date1);
                        i = startPos - 1;
                        continue;
                    }                   
                }

                bool isParsDate2 = DateTime.TryParse(dtl.Data[i][dtl.ColumnDate], out date2);
                bool isParsDepth2 = false;
                if (isParsDate2)
                {
                    isParsDepth2 = double.TryParse(dtl.Data[i][dtl.ColumnZab], NumberStyles.Any, CultureInfo.CurrentCulture, out depth2);
                    if (!isParsDepth2)
                    {
                        continue; //depth2 /= 100; //Math.Round((depth2 / 100), 2, MidpointRounding.ToNegativeInfinity);
                    }
                }
                else
                {
                    continue;
                }               

                double tempD2 = Math.Round(depth2, 1, MidpointRounding.ToNegativeInfinity);
                double tempD1 = Math.Round(depth1, 1, MidpointRounding.ToNegativeInfinity);
                //double delta2 = tempD2 - tempD1;

                double deltaDepth = Math.Round(tempD2 - tempD1, 1);//Math.Round(depth2 - depth1, 1);
                TimeSpan deltaTime = date2 - date1;
                if (deltaDepth > 2 || deltaDepth < -2)
                {
                    if (IsUseStat)
                    {
                        startPos = i;
                        FindFirstDepth(ref startPos, endPos, ref depth1, ref date1);
                        i = startPos - 1;
                        continue;
                    }
                    else
                    {
                        depth1 = depth2;
                        date1 = date2;
                        continue;
                    }                    
                }
                else if (deltaDepth >= 0.1)
                {                    
                    int k = (int)(Math.Round((Math.Abs(deltaDepth) / 0.1)));
                    double tempDelta = Math.Round(depth2 - depth1, 2);                    
                    for (int j = 0; j < k; j++)
                    {
                        double depth = Math.Round(depth1, 1, MidpointRounding.ToPositiveInfinity) + Math.Round(0.1 * j, 1);
                        var d = Math.Round(depth - depth1, 2);
                        var dt = date1 + (d / tempDelta) * deltaTime;
                        listDepthTime.Add((Math.Round(depth, 1), dt));                    
                    }
                    depth1 = depth2;
                    date1 = date2;
                }
                else if (deltaDepth <= -0.1)
                {
                    int k = (int)(Math.Round((Math.Abs(deltaDepth) / 0.1)));                 
                    double tempDelta = Math.Round(depth1 - depth2,2);                    
                    for (int j = 0; j < k; j++)
                    {
                        double depth = Math.Round(depth1, 1, MidpointRounding.ToNegativeInfinity) - Math.Round(0.1 * j, 1);
                        var d = Math.Round(depth1 - depth, 2);
                        var dt = date1 + (d / tempDelta) * deltaTime;                        
                        listDepthTime.Add((Math.Round(depth, 1), dt));                        
                    }
                    depth1 = depth2;
                    date1 = date2;
                }
                else
                {
                    date1 = date2;
                    depth1 = depth2;
                }
            }  
            
            // если используем одну точку на значение глубины
            if (IsOnePoint)
            {
                return listDepthTime.DistinctBy(x => x.Item1).ToList();
            }
            // если используем несколько точек на значение глубины
            else
            {
                return listDepthTime;
            }                                
            
            // локальный метод поиска начала интервала для опции "использовать актив и наж состояние"
            void FindFirstDepth(ref int startPos, int endPos, ref double depth1, ref DateTime date1)
            {
                bool isEndData = true;
                for (int i = startPos; i < endPos; i++)
                {
                    if (dtl.Data[i][dtl.ColumnStat].Trim() == "актив" && dtl.Data[i][dtl.ColumnStat + 1].Trim() == "наж")
                    {
                        bool isParsDate = DateTime.TryParse(dtl.Data[i][dtl.ColumnDate], out date1);
                        if (isParsDate)
                        {
                            bool isParsDepth = double.TryParse(dtl.Data[i][dtl.ColumnZab], NumberStyles.Any, CultureInfo.CurrentCulture, out depth1);
                            if (isParsDepth)
                            {
                                startPos = i + 1;
                                isEndData = false;                                
                                break;
                            }
                        }
                    }
                }
                if (isEndData)
                {
                    startPos = endPos;
                }                
            }
        }
        private void RecordDataLas2(double nullValue, SortedDictionary<double, List<double>> data)
        {
            StringBuilder headLasFile = FormHeadLasFile(data.ElementAt(0).Key, data.ElementAt(data.Count-1).Key);

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "LAS files|*.las";
            saveFileDialog.Title = "Сохранение";

            if (saveFileDialog.ShowDialog() == true)
            {
                string path = saveFileDialog.FileName;

                //записываем инф. шапку
                try
                {
                    using (var writer = new StreamWriter(path, false))
                    {
                        writer.WriteLine($"{headLasFile}");
                    }
                }
                catch (Exception exception)
                {
                    ScrollStatusLasTextBox(exception.Message);
                }

                // записываем данные
                foreach (var item in data)
                {
                    if (data[item.Key].Count == 0)
                    {
                        data[item.Key].Add(nullValue);
                    }
                    try
                    {
                        using (var writer = new StreamWriter(path, true))
                        {
                            writer.WriteLine($"{item.Key.ToString("0.00", CultureInfo.GetCultureInfo("en-US"))}\t\t{Math.Round(item.Value.Average(), 2).ToString("0.00", CultureInfo.GetCultureInfo("en-US"))}");
                        }
                    }
                    catch (Exception exception)
                    {
                        ScrollStatusLasTextBox(exception.Message);
                    }
                }               
                ScrollStatusLasTextBox($"Файл сохранен {saveFileDialog.FileName}");
                MessageBox.Show("Las файл успешно сформирован!");
            }
            else
            {
                ScrollStatusLasTextBox("Файл не сохранен");
            }
        }
        // формирование данных для las файла из данных файла глубиномера
        private SortedDictionary<double, List<double>> GetDataForLasType2(List<(double, DateTime)> listDepthDate)
        {
            SortedDictionary<double, List<double>> depthAndKp = new SortedDictionary<double, List<double>>();
            var choisedCoef = MyCalibrFile.CoefsCalibr[MyCalibrFile.CurrentChoise];
            int savePos = 0;
            TimeSpan timeSpan = TimeSpan.FromSeconds(3);            

            for (int i = 0; i < listDepthDate.Count; i++)
            {
                DateTime timeDepth = listDepthDate[i].Item2; // берем время с глубины время               
                depthAndKp.TryAdd(listDepthDate[i].Item1, new List<double>());
          
                for (int j = savePos; j < DataRowAVM.Count; j++)
                {
                    DataRow row = DataRowAVM[j]; // берем каждую строку из флешки
                    var isTimeFlash= DateTime.TryParse(row["дата"].ToString(), out DateTime timeFlash); // берем время с флешки                    
                    if (!isTimeFlash) continue;
                    
                    if (timeDepth >= timeFlash - timeSpan && timeDepth < timeFlash + timeSpan)
                    {
                        // вычисляем МЗ и БЗ и находим КП
                        double KP1 = 0;
                        double mz = double.Parse(row["ННК1/_ННК1(вода)"].ToString()) / 333.0;
                        double bz = double.Parse(row["ННК2/_ННК2(вода)"].ToString()) / 33.0;
                        
                        if (bz != 0)
                        {
                            double x = mz / bz;
                            if (choisedCoef.Length == 2)
                            {
                                KP1 = choisedCoef[0] * x + choisedCoef[1];
                            }
                            else if (choisedCoef.Length == 3)
                            {
                                KP1 = choisedCoef[0] * x * x + choisedCoef[1] * x + choisedCoef[2];
                            }                                                     
                            depthAndKp[listDepthDate[i].Item1].Add(KP1);
                        }

                        savePos = j--;
                        break;
                    }                     
                    else if(timeFlash + timeSpan > timeDepth)
                    {                       
                        savePos = j--;
                        break;
                    }
                }                
            }
            return depthAndKp;
        }

        // Проверка выбора калибровочных настроек;
        void ChoiseCalibrData()
        {
            string typeOfCalc;
            string diamTruba = lb1_truba.Text;

            if (isLineCalc)
            {
                typeOfCalc = "линейная зависимость";               
            }
            else
            {
                typeOfCalc = "квадратичная зависимость";                
            }

            var trubaAndZav = MyCalibrFile.TrubaZav;

            for (int i = 0; i < trubaAndZav.Count; i++)
            {

                if (trubaAndZav[i][0].Contains(diamTruba) && typeOfCalc.Contains(trubaAndZav[i][1]))
                {
                    MyCalibrFile.CurrentChoise = i;                   
                    break;
                }               
            }

            if (MyCalibrFile.CurrentChoise == -1)
            {
                throw new Exception("Ошибка поиска калибровочных данных: нет подходящих значений");
            }

        }

        // метод дробления данных глубины забоя (метра) и времени
        private SortedDictionary<DateTime, double> UpdateDepthDate()
        {
            SortedDictionary<DateTime,double> DepthTimeDetail = new SortedDictionary<DateTime, double>();
            int posTime = -1;
            int posDepth = -1;            

            for (int i = 0; i < dtl.Data[0].Length; i++)
            {
                if (dtl.ColNames[i].ToLower().Trim() == "забой")
                    posDepth = i;
                if (dtl.ColNames[i].ToLower().Trim() == "дата")
                    posTime = i;
            }

            if (posTime != -1 && posDepth != -1)
            {
                int shift = 0;
                int lastCount = 0;
                double depthStart = 0;
                DateTime timeStart = new DateTime();
                if (IsSetInterval)
                {
                    if (EndTimeRead <= StartTimeRead)
                    {
                        throw new Exception("Считать данные => Стартовое время диапазона должно быть меньше конечного времени");
                    }

                    // Поиск строки которая по времени >= чем StartTimeRead
                    for (int i = 1; i < dtl.Data.Count; i++)
                    {
                        if (DateTime.Parse(dtl.Data[i][posTime]) >= StartTimeRead)
                        {
                            depthStart = double.Parse(dtl.Data[i][posDepth]);
                            timeStart = DateTime.Parse(dtl.Data[i][posTime]);
                            shift = i - 1;
                            break;
                        }
                    }

                    // Поиск строки которая по времени <= чем EndTimeRead
                    for (int i = shift + 1; i < dtl.Data.Count; i++)
                    {
                        DateTime timeFinish = DateTime.Parse(dtl.Data[i][posTime]);
                        if (timeFinish > EndTimeRead)
                        {
                            lastCount = i - 1;
                            break;
                        }
                        else if (timeFinish == EndTimeRead)
                        {
                            lastCount = i;
                        }
                    }

                    if (depthStart == 0)
                    {
                        ScrollStatusLasTextBox("Стартовое значение времени указано не верно");                        
                    }
                    if (lastCount == 0)
                    {
                        ScrollStatusLasTextBox("Конечное значение времени указано не верно");                        
                        lastCount = dtl.Data.Count;
                    }
                }
                else
                {
                    depthStart = double.Parse(dtl.Data[1][posDepth]);
                    timeStart = DateTime.Parse(dtl.Data[1][posTime]);
                    lastCount = dtl.Data.Count;
                }


                DepthTimeDetail.Add(timeStart, depthStart);
                for (int i = 2 + shift; i < lastCount; i++)
                {
                    for (int j = i; j < lastCount; j++)
                    {
                        int depthStop = int.Parse(dtl.Data[j][posDepth]);
                        DateTime timeStop = DateTime.Parse(dtl.Data[j][posTime]);

                        if (depthStop > depthStart && depthStop - depthStart <= 10)
                        {
                            double deltaDepth = depthStop - depthStart;
                            DateTime time2 = timeStop;
                            DateTime time1 = timeStart;
                            TimeSpan deltaTime = time2 - time1;
                            double timeStep = 1.0 / (deltaDepth * 10);
                            double time3 = timeStep;
                            int stepFinish = (int)(deltaDepth / 0.1);
                            double depthStep = 0.1;
                            for (int k = 1; k <= stepFinish; k++) // округление, проблема
                            {
                                double newDepth = depthStart + depthStep;
                                depthStep = Math.Round(depthStep += 0.1, 1);
                                DateTime newTime = time1 + (deltaTime * time3);
                                time3 += timeStep;
                                DepthTimeDetail.Add(newTime, newDepth);
                            }
                            depthStart = depthStop;
                            timeStart = timeStop;
                        }
                        else if (depthStop < depthStart && depthStart - depthStop <= 10)
                        {
                            double deltaDepth = depthStart - depthStop;
                            DateTime time2 = timeStop;
                            DateTime time1 = timeStart;
                            TimeSpan deltaTime = time2 - time1;
                            if (deltaDepth / 0.1 > 60 && deltaTime <= TimeSpan.FromMinutes(1))
                            {
                                continue;
                            }
                            double timeStep = 1.0 / (deltaDepth * 10);
                            double depthStep = 0.1;
                            int stepFinish = (int)(deltaDepth / 0.1);
                            double time3 = timeStep;
                            for (int k = 1; k <= stepFinish; k++)
                            {
                                double newDepth = depthStart - depthStep;
                                depthStep = Math.Round(depthStep += 0.1, 1);
                                DateTime newTime = time1 + (deltaTime * time3);
                                time3 += timeStep;
                                DepthTimeDetail.Add(newTime, newDepth);
                            }
                            depthStart = depthStop;
                            timeStart = timeStop;
                        }
                        else if (depthStart != depthStop)
                        {
                            DepthTimeDetail.Add(timeStop, depthStop);
                            depthStart = depthStop;
                            timeStart = timeStop;
                        }
                        i++;
                    }
                }

            }
            return DepthTimeDetail;
        }

        // метод поиска соответствия данных с флеш с данными из файла глубина время
        List<KeyValuePair<double, List<double>>> GetDataForLasType1(SortedDictionary<DateTime, double> dictTimeDepth)
        {          
            SortedDictionary<double, List<double>> fileDepthAndKP = new SortedDictionary<double, List<double>>();

            var listTimeAndDepth = dictTimeDepth.ToList();
            dictTimeDepth.Clear();

            DateTime timeStartMetr = listTimeAndDepth[0].Key;
            double depthStartMetr = listTimeAndDepth[0].Value;
            DateTime timeEndMetr = new DateTime();
            TimeSpan deltaTime = new TimeSpan();
            int startPosDepTime = 0; // позиция начала метра в файле глубина и время
            int endPosDepTime = 0;
            int posLastValue = 0;
            int dopusk = 0;

            StartTimeRead = new DateTime();

            for (int i = 1; i < listTimeAndDepth.Count; i++)
            {
                double depthEndMetr = listTimeAndDepth[i].Value;

                if (Math.Abs(depthEndMetr - depthStartMetr) == 1)
                {
                    timeEndMetr = listTimeAndDepth[i - 1].Key;
                    endPosDepTime = i - 1;
                }
                else if (Math.Abs(depthStartMetr - depthEndMetr) > 1)
                {
                    depthStartMetr = depthEndMetr;
                    startPosDepTime = i;
                    timeStartMetr = listTimeAndDepth[startPosDepTime].Key;
                    timeEndMetr = new DateTime();
                }

                if (timeStartMetr != new DateTime() && timeEndMetr != new DateTime())
                {
                    int startPosFlashTime = -1; // стартовая позиция в файле флеш
                    int endPosFlashTime = -1; // конец метра в файле флеш

                    for (int j = posLastValue; j < DataRowAVM.Count; j++)
                    {
                        DataRow row = DataRowAVM[j];
                        var timeValue = row["[Время/Дата]"].ToString(); // время во флешке
                        DateTime timeValueRow = new DateTime();
                        if (timeValue != null)
                        {
                            if (!DateTime.TryParse(timeValue, out timeValueRow))
                            {
                                continue;
                            }
                            // ищем позицию по времени начала метра в файле флеш
                            if (startPosFlashTime == -1 && timeValueRow >= timeStartMetr - deltaTime && timeValueRow - timeStartMetr <= TimeSpan.FromSeconds(60))
                            {
                                dopusk = 0;
                                startPosFlashTime = j;
                            }
                            // ищем позицию по времени конца метра в файле флеш
                            else if (endPosFlashTime == -1 && timeValueRow > timeEndMetr && timeValueRow - timeEndMetr <= TimeSpan.FromSeconds(60))
                            {
                                DateTime timePrevious = new DateTime();
                                DataRow rowPrev = DataRowAVM[j - 1];
                                timePrevious = DateTime.Parse(rowPrev["[Время/Дата]"].ToString());
                                if (timeValueRow - timePrevious <= TimeSpan.FromSeconds(60))
                                {
                                    endPosFlashTime = j - 1;
                                }
                                else
                                {
                                    endPosFlashTime = j;
                                }
                                depthStartMetr = depthEndMetr;
                                dopusk = 0;
                                break;
                            }
                            // если мы вышли за временные пределы начала метра
                            // и не нашли стартовое временное значение
                            else if (startPosFlashTime == -1 && timeValueRow - timeStartMetr > TimeSpan.FromSeconds(60))
                            {
                                dopusk++;

                                if (startPosDepTime + dopusk == endPosDepTime)
                                {
                                    break;
                                }
                                timeStartMetr = listTimeAndDepth[startPosDepTime + dopusk].Key;
                                j--;
                            }
                            // если вышли за пределы конца метра более чем на минуту
                            // но нашли стартовое временное значение
                            else if (startPosFlashTime != -1 && endPosFlashTime == -1 && timeValueRow - timeEndMetr > TimeSpan.FromSeconds(60))
                            {
                                endPosFlashTime = j - 1;
                                j--;
                                break;
                            }
                        }
                        posLastValue++;
                    }
                    // добавляем метры без данных в файле флеш
                    if (startPosFlashTime == -1 && endPosFlashTime == -1)
                    {
                        for (int k3 = startPosDepTime; k3 <= endPosDepTime; k3++)
                        {
                            List<double> emptyList = new List<double>();
                            fileDepthAndKP.TryAdd(listTimeAndDepth[k3].Value, emptyList);
                        }
                        startPosDepTime = endPosDepTime;
                        timeStartMetr = listTimeAndDepth[endPosDepTime + 1].Key;
                        depthStartMetr = depthEndMetr;
                        timeEndMetr = new DateTime();
                    }
                    else
                    {
                        for (int k1 = startPosDepTime; k1 <= endPosDepTime; k1++)
                        {
                            if (StartTimeRead == new DateTime())
                            {
                                StartTimeRead = listTimeAndDepth[k1].Key;
                            }

                            DateTime a = new DateTime();
                            DateTime b = new DateTime();

                            if (k1 == startPosDepTime)
                            {
                                if (deltaTime > TimeSpan.FromMinutes(1))
                                {
                                    deltaTime = TimeSpan.FromMinutes(1);
                                }
                                a = listTimeAndDepth[k1].Key - deltaTime;
                                deltaTime = (listTimeAndDepth[startPosDepTime + 1].Key - listTimeAndDepth[startPosDepTime].Key) / 2;

                                b = listTimeAndDepth[k1].Key + deltaTime;
                            }
                            else if (k1 > 0 && k1 < endPosDepTime)
                            {
                                a = listTimeAndDepth[k1].Key - deltaTime;
                                b = listTimeAndDepth[k1].Key + deltaTime;
                            }
                            else
                            {
                                a = listTimeAndDepth[k1].Key - deltaTime;
                                b = listTimeAndDepth[k1].Key;
                            }

                            List<double> KPs = new List<double>();

                            // перебираем данные флеш файла по времени
                            var choisedCoef = MyCalibrFile.CoefsCalibr[MyCalibrFile.CurrentChoise];
                            for (int k2 = startPosFlashTime; k2 <= endPosFlashTime; k2++)
                            {
                                DataRow rowFl = DataRowAVM[k2];
                                var timeFl = DateTime.Parse(rowFl["[Время/Дата]"].ToString());

                                if (timeFl >= a && timeFl < b)
                                {
                                    double mz = double.Parse(rowFl["[ННК1/\nННК1(вода)]"].ToString()) / 333;
                                    double bz = double.Parse(rowFl["[ННК2/\nННК2(вода)]"].ToString()) / 33;
                                    double x;
                                    double KP = 0;
                                    if (bz != 0)
                                    {
                                        x = mz / bz;
                                        if (choisedCoef.Length == 2)
                                        {
                                            KP = choisedCoef[0] * x + choisedCoef[1];
                                        }
                                        else if (choisedCoef.Length == 3)
                                        {
                                            KP = choisedCoef[0] * x * x + choisedCoef[1] * x + choisedCoef[2];
                                        }
                                        startPosFlashTime++;
                                        KPs.Add(KP);
                                    }
                                    else
                                    {
                                        startPosFlashTime++;
                                        continue;
                                    }
                                }
                                else if (timeFl > b)
                                {
                                    break;
                                }
                            }

                            if (!fileDepthAndKP.TryAdd(listTimeAndDepth[k1].Value, KPs))
                            {
                                fileDepthAndKP[listTimeAndDepth[k1].Value].AddRange(KPs);
                            }
                        }
                        depthStartMetr = depthEndMetr;
                        startPosDepTime = i;

                        timeStartMetr = listTimeAndDepth[startPosDepTime].Key;
                        timeEndMetr = new DateTime();
                        dopusk = 0;
                    }
                }
            }

            var list2 = fileDepthAndKP.ToList();
            if (list2.Count == 0)
            {
                throw new Exception("Отсутствуют данные для записи");
            }
            fileDepthAndKP.Clear();
            return list2;
        }

        // запись данных в файл las для старого типа файлов глубина-время
        private void RecordDataLas(double nullValue, List<KeyValuePair<double, List<double>>> list2)
        {
            StringBuilder headLasFile = FormHeadLasFile(list2[0].Key, list2[list2.Count - 1].Key);

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "LAS files|*.las";
            saveFileDialog.Title = "Сохранение";

            if (saveFileDialog.ShowDialog() == true)
            {
                string path = saveFileDialog.FileName;

                // записываем инф. шапку
                try
                {
                    using (var writer = new StreamWriter(path, false))
                    {
                        writer.WriteLine($"{headLasFile.ToString()}");
                    }
                }
                catch (Exception exception)
                {
                    ScrollStatusLasTextBox(exception.Message);
                }

                // записываем данные
                for (int i = 0; i < list2.Count; i++)
                {
                    if (list2[i].Value.Count == 0)
                    {
                        list2[i].Value.Add(nullValue);
                    } 
                    try
                    {
                        using (var writer = new StreamWriter(path, true))
                        {
                            writer.WriteLine($"{list2[i].Key.ToString("0.00", CultureInfo.GetCultureInfo("en-US"))}     {Math.Round(list2[i].Value.Average(), 2).ToString("0.00", CultureInfo.GetCultureInfo("en-US"))}");
                        }
                    }
                    catch (Exception exception)
                    {
                        ScrollStatusLasTextBox(exception.Message);
                    }
                }
                ScrollStatusLasTextBox($"Файл сохранен {saveFileDialog.FileName}");
                MessageBox.Show("Las файл успешно сформирован!");
            }
            else
            {
                ScrollStatusLasTextBox("Файл не сохранен");
            }
        }

        // Формирование информационной шапки для las файла
        private StringBuilder FormHeadLasFile(double startM, double stopM)
        {
            double stepM = 0.10;
            StringBuilder result = new StringBuilder();
            // Первый раздел
            int countSigns = 8;
            result.AppendLine("~VERSION INFORMATION SECTION");
            result.AppendLine($"VERS.{new string(' ', 11)}{txtBoxVers.Text}" +
                $"{new string(' ', countSigns - txtBoxVers.Text.Length)} :" + $"{new string(' ', 3)}CWLS log ASCII Standard -VERSION 2.0");
            result.AppendLine($"WRAP.{new string(' ', 11)}{txtBoxWrap.Text}" +
                $"{new string(' ', (countSigns - txtBoxWrap.Text.Length))} :" + $"{new string(' ', 3)}One line per depth step");
            result.AppendLine("-----------------------------------------------------------------------------");

            // Второй раздел
            countSigns = 25;
            result.AppendLine("~WELL INFORMATION SECTION");
            result.AppendLine("#MNEM                    .UNIT   VALUE/NAME               : DESCRIPTION");
            result.AppendLine("#----                     ----   -----------------        -----------------");
            result.AppendLine($"STRT                     .M      {startM}{new string(' ', countSigns - startM.ToString().Length)}: START DEPTH");
            result.AppendLine($"STOP                     .M      {stopM}{new string(' ', countSigns - stopM.ToString().Length)}: STOP DEPTH");
            result.AppendLine($"STEP                     .M      {stepM.ToString("0.00")}{new string(' ', countSigns - stepM.ToString("0.00").Length)}: STEP");
            result.AppendLine($"NULL                     .M      {txtBoxNULL.Text}{new string(' ', countSigns - txtBoxNULL.Text.Length)}: NULL VALUE");
            result.AppendLine($"DATE                     .M      {StartTimeRead.ToString("yyyy-MM-dd HH:mm:ss")}{new string(' ', countSigns - 19)}: DATE");
            result.AppendLine($"API                      .       {txtBoxAPI.Text}{new string(' ', countSigns - txtBoxAPI.Text.Length)}: API NUMBER");
            result.AppendLine($"WELL                     .       {txtBoxWell.Text}{new string(' ', countSigns - txtBoxWell.Text.Length)}: WELL");
            result.AppendLine($"FLD                      .       {txtBoxFLD.Text}{new string(' ', countSigns - txtBoxFLD.Text.Length)}: FIELD");
            result.AppendLine($"CTRY                     .       {txtBoxCNTY.Text}{new string(' ', countSigns - txtBoxCNTY.Text.Length)}: COUNTRY");
            result.AppendLine($"STAT                     .       {txtBoxSTATE.Text}{new string(' ', countSigns - txtBoxSTATE.Text.Length)}: STATE");
            result.AppendLine($"SRVC                     .       {txtBoxSRVC.Text}{new string(' ', countSigns - txtBoxSRVC.Text.Length)}: SERVICE COMPANY");
            result.AppendLine($"TRUBA                    .       {lb1_truba.SelectedItem.ToString()}{new string(' ', countSigns - lb1_truba.SelectedItem.ToString().Length)}: DIAMETR TRUBI");

            string typeCalc = rb2_Kvad.IsChecked == true ? "квад.завис" : "лин.зав";
            result.AppendLine($"CALC_W                   .       {typeCalc}{new string(' ', countSigns - typeCalc.Length)}: TYPE CALCULATE");
            string fileCreadted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            result.AppendLine($"FILECREATED              .       {fileCreadted}{new string(' ', countSigns - fileCreadted.Length)}: DATE");
            result.AppendLine("#-----------------------------------------------------------------------------");

            // Третий раздел            
            result.AppendLine("~CURVE INFORMATION SECTION");
            result.AppendLine("MD                       .M     :DEPTH");
            result.AppendLine("W                        .%     :CoefPoristosti");
            result.AppendLine("#-----------------------------------------------------------------------------");

            // Четвертый раздел    
            result.AppendLine("#  MD         W ");
            result.Append("~ASCII Log Data");
            return result;
        }

        private void ScrollStatusLasTextBox(string message)
        {
            StatusLasMenu += $"{DateTime.Now}: {message} \n";
            txtBoxStatusLas.ScrollToEnd();
        }

        // кнопка загрузки файла глубина-время
        //void btn_LoadDepthAndTime_Click(object sender, RoutedEventArgs e)
        //{           
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "Файл глубина-время|*.txt";
        //    openFileDialog.Title = "Выберите файл с глубиной и временем";
        //    string path;
        //    bool isTable = false;
        //    int countHeaders = 0;
        //    int posDate = 0;            
        //    int year = 0;
        //    int month = 0;
        //    // выводим данные из файла на экран
        //    if (openFileDialog.ShowDialog()==true)
        //    {
        //        FileDepthAndTime = new List<string[]>();
        //        path = openFileDialog.FileName;
        //        ScrollStatusLasTextBox($"Загрузка файла началась: {openFileDialog.SafeFileName}");               

        //        try
        //        {
        //            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);                   
        //            using (var reader = new StreamReader(path, Encoding.GetEncoding(1251)))
        //            {
        //                while (!reader.EndOfStream)
        //                {
        //                    var row = reader.ReadLine();
        //                    if (!string.IsNullOrEmpty(row))
        //                    {
        //                        // ищем год по выражению
        //                        if (row.ToLower().Contains("данные с"))
        //                        {
        //                            string[] arrayDate = row.Split();
        //                            foreach (string date in arrayDate)
        //                            {
        //                                if (DateTime.TryParse(date, out DateTime result))
        //                                {
        //                                    year = result.Year;
        //                                    month = result.Month;
        //                                    break;
        //                                }                                      
        //                            }
        //                        }
        //                        // ищем начало названия столбцов
        //                        if (row.Contains('|') && row.Contains("Забой"))
        //                        {
        //                            isTable = true;
        //                            var tempArray = row.Split('|', StringSplitOptions.RemoveEmptyEntries);
        //                            for (int i = 0; i < tempArray.Length; i++)
        //                            {
        //                                if (tempArray[i].ToLower().Trim() == "дата" && tempArray[i-1].ToLower().Trim() == "время")
        //                                {
        //                                    posDate = i;
        //                                }
        //                            }
        //                            countHeaders = tempArray.Length;
        //                            tempArray[posDate - 1] = "Дата";
        //                            FileDepthAndTime.Add(tempArray.SkipLast(1).ToArray());
        //                        }                                
        //                        // записываем табличные данные
        //                        if (isTable)
        //                        {
        //                            string[] array = row.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).ToArray(); // массив с данными
        //                            // проверка начала численных данных
        //                            bool isTableData = int.TryParse(array[0], out int temRes); 
        //                            if (isTableData)
        //                            {
        //                                var dayAndMOnth = array[posDate].Split('.');
        //                                if (int.TryParse(dayAndMOnth[1], out int resultMonth) && resultMonth < month)
        //                                {
        //                                    month = resultMonth;
        //                                    year++;
        //                                }

        //                                array[posDate-1] = $"{array[array.Length - 2]}:00 {array[array.Length - 1]}.{year}";                                        
        //                                if (array.Length == countHeaders)
        //                                {
        //                                    FileDepthAndTime.Add(array.SkipLast(1).ToArray());
        //                                }
        //                            }                
        //                        }
        //                    }
        //                }
        //            }
        //            lblDepthFile.Content = openFileDialog.SafeFileName;
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //        }

        //        PercentLas = 60;
        //        DataTable dt = new DataTable();
        //        if (FileDepthAndTime.Count == 0)
        //        {
        //            MessageBox.Show("Возникла ошибка чтения файла");
        //            PercentLas = 0;
        //            return;
        //        }

        //        byte counterColumn = 1;                

        //        for (int i = 0; i < FileDepthAndTime[0].Length; i++)
        //        {
        //            string nameCol1 = FileDepthAndTime[0][i];
        //            for (int j = i + 1; j < FileDepthAndTime[0].Length-1; j++)
        //            {
        //                string nameCol2 = FileDepthAndTime[0][j];
        //                if (nameCol2 == nameCol1)
        //                {
        //                    FileDepthAndTime[0][j] = $"{FileDepthAndTime[0][j].Trim()}{++counterColumn}"; 
        //                }                       
        //            }
        //            if (counterColumn != 1)
        //            {
        //                FileDepthAndTime[0][i] = $"{FileDepthAndTime[0][i].Trim()}1";
        //                counterColumn = 1;
        //            }
        //        }

        //        for (int i = 0; i < FileDepthAndTime[0].Length; i++)
        //        {
        //            DataColumn dataColumn = new DataColumn();
        //            dataColumn.ColumnName = FileDepthAndTime[0][i].Trim();                  
        //            dt.Columns.Add(dataColumn);
        //        }

        //        if (FileDepthAndTime.Count>2)
        //        {                    
        //            for(int i = 1; i < FileDepthAndTime.Count; i++)
        //            {
        //                var rowTable = dt.NewRow();
        //                for (int j = 0; j < FileDepthAndTime[i].Length; j++)
        //                {
        //                    rowTable[j] = FileDepthAndTime[i][j];
        //                }
        //                dt.Rows.Add(rowTable);
        //            }
        //        }
        //        else
        //        {
        //            ScrollStatusLasTextBox("Файл не содержит достаточное количество данных");                                       
        //        }

        //        //OpenCalibrFile();                
        //        DataTable = dt;

        //        PercentLas = 100;

        //        ScrollStatusLasTextBox("Загрузка завершена");               
        //        StartTimeRead = DateTime.Parse(dt.Rows[0]["Дата"].ToString());
        //        EndTimeRead = DateTime.Parse(dt.Rows[dt.Rows.Count-1]["Дата"].ToString());
        //        IsMoveTime = true;
        //        PercentLas = 0;
        //    }
        //    IsOpenFile = true;
        //}

        // кнопка загрузки двух типов файлов глубина-время: с глубиномера
        // и старый формат с дискретность метр из программы InfoFrom

        void btn_LoadGlubDate_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "Файл глубина-время";
            openFileDialog.Title = "Выберите файл с глубиной и временем";
            openFileDialog.Multiselect = true;

            // выводим данные из файла на экран
            if (openFileDialog.ShowDialog() == true)
            {
                lblDepthFile.Text = "";
                var paths = openFileDialog.FileNames;
                ScrollStatusLasTextBox($"Загрузка файла началась: {openFileDialog.SafeFileName}");
                dtl = new DepthTimeLas();
                dtl.FillStandColumn();
                List<byte> resultData = new List<byte>();
                try
                {
                    foreach (var filePath in paths)
                    {
                        resultData.AddRange(File.ReadAllBytes(filePath).Skip(256));
                        var pathFile = filePath.Split("\\");
                        lblDepthFile.Text += pathFile[pathFile.Length - 1] + "\n";
                    }

                    for (int i = 0; i < resultData.Count; i = i + 16) // 16 байт в одной строке
                    {
                        int flag = BitConverter.ToInt16(resultData.Skip(i + 12).Take(2).ToArray());
                        //long x = BitConverter.ToInt64(resultData.Skip(i).Take(8).ToArray());
                        double x = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(resultData.Skip(i).Take(8).ToArray()));
                        DateTime dateLine = DateTime.FromOADate(x);
                        double depth = (BitConverter.ToInt32(resultData.Skip(i + 8).Take(4).ToArray())) * 1.0 / 100;
                        string activ = "неопр.";
                        string push = "неопр.";
                        switch (flag)
                        {
                            case 0:
                                activ = "неактив";
                                push = "отж";
                                break;
                            case 1:
                                activ = "актив";
                                push = "отж";
                                break;
                            case 2:
                                activ = "неактив";
                                push = "наж";
                                break;
                            case 3:
                                activ = "актив";
                                push = "наж";
                                break;
                            default:
                                break;
                        }
                        string[] arr = new string[5] { dateLine.ToString("G"), dateLine.Millisecond.ToString(), depth.ToString(), activ, push };
                        dtl.Data.Add(arr);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка чтения файла");
                    ScrollStatusLasTextBox(ex.Message);
                    return;
                }

                PercentLas = 60;
                DataTable dt = new DataTable();

                if (dtl.Data.Count == 0)
                {
                    MessageBox.Show("Возникла ошибка чтения файла");
                    PercentLas = 0;
                    return;
                }

                EditColAndFillTalbe(dtl, dt);
                DataTable = dt;
                PercentLas = 100;

                ScrollStatusLasTextBox("Загрузка завершена");
                StartTimeRead = DateTime.Parse(dt.Rows[0]["Дата"].ToString());
                EndTimeRead = DateTime.Parse(dt.Rows[dt.Rows.Count - 1]["Дата"].ToString());
                IsMoveTime = true;
                PercentLas = 0;
            }
            IsOpenFile = true;
            grLimit.IsEnabled = true;
            // Процедура проверки одинаковых имен столбцов
            // И заполнение табличных данных
            void EditColAndFillTalbe(DepthTimeLas dtf, DataTable dt)
            {
                byte counterColumn = 1;

                for (int i = 0; i < dtf.ColNames.Count; i++)
                {
                    string nameCol1 = dtf.ColNames[i];
                    for (int j = i + 1; j < dtf.ColNames.Count - 1; j++)
                    {
                        string nameCol2 = dtf.ColNames[j];
                        if (nameCol2 == nameCol1)
                        {
                            dtf.ColNames[j] = $"{dtf.ColNames[j].Trim()}{++counterColumn}";
                        }
                    }
                    if (counterColumn != 1)
                    {
                        dtf.ColNames[i] = $"{dtf.ColNames[i].Trim()}1";
                        counterColumn = 1;
                    }
                }

                for (int i = 0; i < dtf.ColNames.Count; i++)
                {
                    DataColumn dataColumn = new DataColumn();
                    dataColumn.ColumnName = dtf.ColNames[i].Trim();
                    dt.Columns.Add(dataColumn);
                }

                if (dtf.Data.Count > 2)
                {
                    for (int i = 0; i < dtf.Data.Count; i++)
                    {
                        var rowTable = dt.NewRow();
                        for (int j = 0; j < dtf.Data[i].Length; j++)
                        {
                            rowTable[j] = dtf.Data[i][j];
                        }
                        dt.Rows.Add(rowTable);
                    }
                }
                else
                {
                    ScrollStatusLasTextBox("Файл не содержит достаточное количество данных");
                }
            }

        }
        void btn_LoadDepthAndTime_Click2(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файл глубина-время|*.txt";
            openFileDialog.Title = "Выберите файл с глубиной и временем";
            openFileDialog.Multiselect = true;         
                       
            // выводим данные из файла на экран
            if (openFileDialog.ShowDialog() == true)
            {
                lblDepthFile.Text = "";                
                var paths = openFileDialog.FileNames;
                ScrollStatusLasTextBox($"Загрузка файла началась: {openFileDialog.SafeFileName}");
                dtl = new DepthTimeLas();                

                try
                {
                    foreach (var filePath in paths)
                    {
                        bool isData = false;

                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        using (var reader = new StreamReader(filePath, Encoding.GetEncoding(1251)))
                        {
                            while (!reader.EndOfStream)
                            {
                                var row = reader.ReadLine();
                                if (!string.IsNullOrEmpty(row))
                                {
                                    if (!isData)
                                    {
                                        if (row.Contains("Дата") && row.Contains("Время") && row.Contains("Забой") && row.Contains("|"))
                                        {
                                            if (string.IsNullOrEmpty(dtl.Year))
                                            {
                                                break;
                                            }
                                            dtl.Separator = '|';
                                            grLimit.IsEnabled = false;
                                            dtl.LoadDepthFromZtkProg(row);
                                            isData = true;
                                            continue;
                                        }
                                        else if (row.Contains("дата") && row.Contains("время") && row.Contains("забой") && !row.Contains("|"))
                                        {
                                            dtl.Separator = ' ';
                                            grLimit.IsEnabled = true;
                                            dtl.LoadDepthFromGlub(row);
                                            isData = true;
                                            continue;
                                        }
                                        // ищем год по выражению
                                        else if (row.ToLower().Contains("данные с"))
                                        {
                                            string[] arrayDate = row.Split();
                                            foreach (string date in arrayDate)
                                            {
                                                if (DateTime.TryParse(date, out DateTime result))
                                                {
                                                    dtl.Year = result.Year.ToString();
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    // записываем табличные данные
                                    else
                                    {
                                        dtl.addData.Invoke(row);
                                    }
                                }
                            }
                        }
                        var pathFile = filePath.Split("\\");
                        lblDepthFile.Text += pathFile[pathFile.Length - 1] + "\n";
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка чтения файла");
                    ScrollStatusLasTextBox(ex.Message);
                }

                PercentLas = 60;
                DataTable dt = new DataTable();

                if (dtl.Data.Count == 0)
                {
                    MessageBox.Show("Возникла ошибка чтения файла");
                    PercentLas = 0;
                    return;
                }

                EditColAndFillTalbe(dtl, dt);
                DataTable = dt;
                PercentLas = 100;

                ScrollStatusLasTextBox("Загрузка завершена");
                StartTimeRead = DateTime.Parse(dt.Rows[0]["Дата"].ToString());
                EndTimeRead = DateTime.Parse(dt.Rows[dt.Rows.Count - 1]["Дата"].ToString());
                IsMoveTime = true;
                PercentLas = 0;
            }
            IsOpenFile = true;

            // Процедура проверки одинаковых имен столбцов
            // И заполнение табличных данных
            void EditColAndFillTalbe(DepthTimeLas dtf, DataTable dt)
            {                
                byte counterColumn = 1;

                for (int i = 0; i < dtf.ColNames.Count; i++)
                {
                    string nameCol1 = dtf.ColNames[i];
                    for (int j = i + 1; j < dtf.ColNames.Count - 1; j++)
                    {
                        string nameCol2 = dtf.ColNames[j];
                        if (nameCol2 == nameCol1)
                        {
                            dtf.ColNames[j] = $"{dtf.ColNames[j].Trim()}{++counterColumn}";
                        }
                    }
                    if (counterColumn != 1)
                    {
                        dtf.ColNames[i] = $"{dtf.ColNames[i].Trim()}1";
                        counterColumn = 1;
                    }
                }

                for (int i = 0; i < dtf.ColNames.Count; i++)
                {
                    DataColumn dataColumn = new DataColumn();
                    dataColumn.ColumnName = dtf.ColNames[i].Trim();
                    dt.Columns.Add(dataColumn);
                }

                if (dtf.Data.Count > 2)
                {
                    for (int i = 0; i < dtf.Data.Count; i++)
                    {
                        var rowTable = dt.NewRow();
                        for (int j = 0; j < dtf.Data[i].Length; j++)
                        {
                            rowTable[j] = dtf.Data[i][j];
                        }
                        dt.Rows.Add(rowTable);
                    }
                }
                else
                {
                    ScrollStatusLasTextBox("Файл не содержит достаточное количество данных");
                }
            }
        }

        // обработчик заголовков таблицы
        void r2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.Contains('[') || e.PropertyName.Contains(' ') || e.PropertyName.Contains('.') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            }
        }
                     

       // кнопка обновить данные таблицы
        private void btn_UpdateCurrentData_Click(object sender, RoutedEventArgs e)
        {
            int posTime = -1;

            for (int i = 0; i < dtl.Data[0].Length; i++)
            {
                if (dtl.ColNames[i].ToLower().Trim() == "дата")
                {
                    posTime = i;
                    break;
                }
            }

            if (TimeSpan.TryParse(ShiftTime, out TimeSpan ts))
            {
                if (posTime >= 0 && posTime < dtl.Data[0].Length)
                {
                    if (IsMoveTimeUp)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            dataTable.Rows[i]["Дата"] = (DateTime.Parse(dataTable.Rows[i]["Дата"].ToString()) + ts).ToString("dd.MM.yy HH:mm:ss");
                        }
                        for (int i = 1; i < dtl.Data.Count; i++)
                        {
                            dtl.Data[i][posTime] = (DateTime.Parse(dtl.Data[i][posTime]) + ts).ToString("dd.MM.yy HH:mm:ss");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            dataTable.Rows[i]["Дата"] = (DateTime.Parse(dataTable.Rows[i]["Дата"].ToString()) - ts).ToString("dd.MM.yy HH:mm:ss");
                        }
                        for (int i = 1; i < dtl.Data.Count; i++)
                        {
                            dtl.Data[i][posTime] = (DateTime.Parse(dtl.Data[i][posTime]) - ts).ToString("dd.MM.yy HH:mm:ss");
                        }
                    }
                }
                else
                {
                    //StatusLasMenu += $"{DateTime.Now}: Отсутствует столбец с датой";
                    ScrollStatusLasTextBox("Отсутствует столбец с датой");
                }
            }
            else
            {
                ScrollStatusLasTextBox("Не могу преобразовать значение указанное в поле для сдвига времени");
                //StatusLasMenu += $"{DateTime.Now}: Не могу преобразовать значение указанное в поле для сдвига времени";
            }
        }

        private void btn_HeadLasWrite_Click(object sender, RoutedEventArgs e)
        {
            dataTab.Focus();
            //lstBoxLasValues.Focus();
        }

        private void btn_BackToDataGrid_Click(object sender, RoutedEventArgs e)
        {
            filesTab.Focus();
        }
        
        // кнопка открытия калибровочного файла
        private void btn_OpenCalibrFile_Click(object sender, RoutedEventArgs e)
        {
            List<string> FileColibrData = new List<string>();
            OpenFileDialog openCalibrFile = new OpenFileDialog();
            openCalibrFile.Filter = "Калибровочный файл|*.nk";
            openCalibrFile.Title = "Выберите подходящий калибровочный файл";
            // считываем данные из калибровочного файла
            if (openCalibrFile.ShowDialog() == true)
            {
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    // надо переделать выбор калибровочного файла                    
                    using (var reader = new StreamReader(openCalibrFile.FileName, Encoding.GetEncoding(1251)))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                //var splitLine = line.Split(':');
                                //if (splitLine.Length > 1)
                                //{
                                FileColibrData.Add(line);
                                //}
                            }
                        }
                    }
                    MyCalibrFile = new CalibrFile();                    
                    bool isSupportedVers = MyCalibrFile.CheckCalibrVers(FileColibrData);
                    if (!isSupportedVers)
                    {
                        MessageBox.Show("Текущий калибровочный файл не поддерживается");
                        ScrollStatusLasTextBox("Невозможно обработать текущий конфигурационный файл");
                        return;
                    }                   

                }
                catch (Exception ex)
                {
                    StatusLasMenu += $"{DateTime.Now}: {ex.Message}\n";
                    return;
                }                
                ScrollStatusLasTextBox($"Калибровочный файл {openCalibrFile.SafeFileName} успешно считан");
                lblCalibrFile.Text = openCalibrFile.SafeFileName;
                IsOpenCalibFile = true;

                if (IsOpenCalibFile)
                {
                    btnLasStart.IsEnabled = true;
                    //txtBoxShift.IsEnabled = true;
                }
            }
            else
            {
                ScrollStatusLasTextBox("Необходимо выбрать калибровочный файл");                
            }

        }
              
        private void OnClosing(object sender, CancelEventArgs e)
        {
            Owner.Activate();
        }
                
    }
}
