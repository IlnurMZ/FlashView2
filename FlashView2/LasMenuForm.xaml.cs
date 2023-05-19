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

        public List<string[]> FileDepthAndTime { get; set; } // файл с данными по глубине и времени
        public List<string[]> FileColibr { get; set; } // файл с колибровочными данными        
        double[] Coef { get; set; } // коэффициенты для расчета Кп
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

            string[] badVaues = { "N", "[IDустр. /№пакета]", "[Ошибка I2C]", "[конец строки]", "[№]", "[Время/Дата]" };
            lstBoxLasValues.Items.Add("Глубина");
            lstBoxLasValues.Items.Add("Коэф. пористости");
            lstBoxLasValues.Items.Add("Дата");

            lstBoxLasValues.SelectedItems.Add("Глубина");            
            lstBoxLasValues.SelectedItems.Add("Коэф. пористости");
            lstBoxLasValues.SelectedItems.Add("Дата");

            for (int i = 0; i < dt.Columns.Count; i++)
            {                
                if (badVaues.Contains(dt.Columns[i].ToString().Replace("\n", " ")))
                    continue;               
                lstBoxLasValues.Items.Add(dt.Columns[i].ToString().Replace("\n"," "));
            }
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
                ChoiseCalibrData();
                var selectedLasValues = lstBoxLasValues.SelectedItems;
                var timeDepth = UpdateDepthDate();
                FindNearestTimeValue(timeDepth);
                
            }
            catch (Exception ex)
            {
                ScrollStatusLasTextBox(ex.Message);
                //StatusLasMenu += $"{DateTime.Now}: {ex.Message}\n";                
            }                              
        }

        // Проверка выбора калибровочных настроек;
        void ChoiseCalibrData()
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
                                throw;                                
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
        }        

        // метод дробления данных глубины забоя (метра) и времени
        SortedDictionary<DateTime, double> UpdateDepthDate()
        {
            SortedDictionary<DateTime,double> DepthTimeDetail = new SortedDictionary<DateTime, double>();
            int posTime = -1;
            int posDepth = -1;

            for (int i = 0; i < FileDepthAndTime[0].Length; i++)
            {
                if (FileDepthAndTime[0][i].Trim() == "Замер")
                    posDepth = i;
                if (FileDepthAndTime[0][i].Trim() == "Дата")
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
                    for (int i = 1; i < FileDepthAndTime.Count; i++)
                    {
                        if (DateTime.Parse(FileDepthAndTime[i][posTime]) >= StartTimeRead)
                        {
                            depthStart = double.Parse(FileDepthAndTime[i][posDepth]);
                            timeStart = DateTime.Parse(FileDepthAndTime[i][posTime]);
                            shift = i - 1;
                            break;
                        }                       
                    }

                    for (int i = shift + 1; i < FileDepthAndTime.Count; i++)
                    {
                        DateTime timeFinish = DateTime.Parse(FileDepthAndTime[i][posTime]);
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
                        //StatusLasMenu += $"{DateTime.Now}: Стартовое значение времени указано не верно\n";
                    }
                    if (lastCount == 0)
                    {
                        ScrollStatusLasTextBox("Конечное значение времени указано не верно");
                        //StatusLasMenu += $"{DateTime.Now}: Конечное значение времени указано не верно\n";
                        lastCount = FileDepthAndTime.Count;
                    }
                }
                else
                {
                    depthStart = double.Parse(FileDepthAndTime[1][posDepth]);
                    timeStart = DateTime.Parse(FileDepthAndTime[1][posTime]);
                    lastCount = FileDepthAndTime.Count;
                }
                
                DepthTimeDetail.Add(timeStart, depthStart);
                for (int i = 2 + shift; i < lastCount; i++)
                {
                    for (int j = i; j < lastCount; j++)
                    {
                        int depthStop = int.Parse(FileDepthAndTime[j][posDepth]);
                        DateTime timeStop = DateTime.Parse(FileDepthAndTime[j][posTime]);

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
        void FindNearestTimeValue(SortedDictionary<DateTime, double> dictTimeDepth)
        {
            if (!double.TryParse(txtBoxNULL.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double nullValue))
            {
                nullValue = -999.99;
                ScrollStatusLasTextBox($"Ошибка конвертации Null value. Установлено дефолтное значение {nullValue}");
                //StatusLasMenu += $"{DateTime.Now}: Ошибка конвертации Null value \n";
            }
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
                            for (int k2 = startPosFlashTime; k2 <= endPosFlashTime; k2++)
                            {
                                DataRow rowFl = DataRowAVM[k2];
                                var timeFl = DateTime.Parse(rowFl["[Время/Дата]"].ToString());

                                if (timeFl >= a && timeFl < b)
                                {
                                    double mz = double.Parse(rowFl["[ННК1/\nННК1(вода)]"].ToString());
                                    double bz = double.Parse(rowFl["[ННК2/\nННК2(вода)]"].ToString());
                                    double x;
                                    double KP = 0;
                                    if (mz != 0)
                                    {
                                        x = bz / mz;
                                        if (Coef.Length == 2)
                                        {
                                            KP = Coef[0] * x + Coef[1];
                                        }
                                        else if (Coef.Length == 3)
                                        {
                                            KP = Coef[0] * x * x + Coef[1] * x + Coef[2];
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

            StringBuilder headLasFile = FormHeadLasFile(list2[0].Key, list2[list2.Count-1].Key);

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "LAS files|*.las";
            saveFileDialog.Title = "Сохранение";

            if (saveFileDialog.ShowDialog() == true)
            {
                string path = saveFileDialog.FileName;

                // записываем инф. шапку
                try
                {
                    using (var writer = new StreamWriter(path, true))
                    {
                        writer.WriteLine($"{headLasFile.ToString()}");
                    }
                }
                catch (Exception exception)
                {
                    ScrollStatusLasTextBox(exception.Message);
                    //StatusLasMenu += $"{DateTime.Now}: Ошибка. {exception.Message}\n";
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
                        //StatusLasMenu += $"{DateTime.Now}: Ошибка. {exception.Message}\n";
                    }
                }
                ScrollStatusLasTextBox($"Файл сохранен {saveFileDialog.FileName}");
                //StatusLasMenu += $"{DateTime.Now}: Файл сохранен {saveFileDialog.FileName}\n";
                MessageBox.Show("Las файл успешно сформирован!");
            }
            else
            {
                ScrollStatusLasTextBox("Файл не сохранен");
                //StatusLasMenu += $"{DateTime.Now}: Файл не сохранен\n";
            }
        }

        // Формирование информационной шапки для Las файла
        StringBuilder FormHeadLasFile(double startM, double stopM)
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
            string fileCreadted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            result.AppendLine($"FILECREATED              .       {fileCreadted}{new string(' ', countSigns - fileCreadted.Length)}: SERVICE COMPANY");
            result.AppendLine("#-----------------------------------------------------------------------------");

            // Третий раздел            
            result.AppendLine("~CURVE INFORMATION SECTION");
            result.AppendLine("MD                       .M     :DEPTH");
            result.AppendLine("W                       .%    :CoefPoristosti");
            result.AppendLine("#-----------------------------------------------------------------------------");

            // Четвертый раздел    
            result.AppendLine("#  MD         W ");
            result.Append("~ASCII Log Data");
            return result;
        }

        void ScrollStatusLasTextBox(string message)
        {
            StatusLasMenu += $"{DateTime.Now}: {message} \n";
            txtBoxStatusLas.ScrollToEnd();
        }

        // кнопка загрузки файла глубина-время
        void btn_LoadDepthAndTime_Click(object sender, RoutedEventArgs e)
        {           
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файл глубина-время|*.txt";
            openFileDialog.Title = "Выберите файл с глубиной и временем";
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
                ScrollStatusLasTextBox($"Загрузка файла началась: {openFileDialog.SafeFileName}");               

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
                    lblDepthFile.Content = openFileDialog.SafeFileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                PercentLas = 60;
                DataTable dt = new DataTable();
                if (FileDepthAndTime.Count == 0)
                {
                    MessageBox.Show("Возникла ошибка чтения файла");
                    PercentLas = 0;
                    return;
                }
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
                    ScrollStatusLasTextBox("Файл не содержит достаточное количество данных");                                       
                }

                //OpenCalibrFile();                
                DataTable = dt;

                PercentLas = 100;

                ScrollStatusLasTextBox("Загрузка завершена");               
                StartTimeRead = DateTime.Parse(dt.Rows[0]["Дата"].ToString());
                EndTimeRead = DateTime.Parse(dt.Rows[dt.Rows.Count-1]["Дата"].ToString());
                IsMoveTime = true;
                PercentLas = 0;
            }
            IsOpenFile = true;
        }      
        
        // обработчик заголовков таблицы
        void r2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.Contains('[') || e.PropertyName.Contains(' ') && e.Column is DataGridBoundColumn)
            {
                DataGridBoundColumn dataGridBoundColumn = e.Column as DataGridBoundColumn;
                dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            }
        }

        // метод сдвига колонки "временя"
        void UseShiftTime_Click()
        {
            int posTime = -1;

            for (int i = 0; i < FileDepthAndTime[0].Length; i++)
            {
                if (FileDepthAndTime[0][i].Trim() == "Дата")
                {
                    posTime = i;
                    break;
                }    
                    
            }

            if (TimeSpan.TryParse(ShiftTime,out TimeSpan ts))
            {
                if (posTime >= 0 && posTime < FileDepthAndTime[0].Length)
                {
                    if (IsMoveTimeUp)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            dataTable.Rows[i]["Дата"] = " " + (DateTime.Parse(dataTable.Rows[i]["Дата"].ToString()) + ts).ToString("HH:mm:ss dd.MM.yy") + " ";
                        }
                        for (int i = 1; i < FileDepthAndTime.Count; i++)
                        {
                            FileDepthAndTime[i][posTime] = (DateTime.Parse(FileDepthAndTime[i][posTime]) + ts).ToString("HH:mm:ss dd.MM.yy");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            dataTable.Rows[i]["Дата"] = " " + (DateTime.Parse(dataTable.Rows[i]["Дата"].ToString()) - ts).ToString("HH:mm:ss dd.MM.yy") + " ";
                        }
                        for (int i = 1; i < FileDepthAndTime.Count; i++)
                        {
                            FileDepthAndTime[i][posTime] = (DateTime.Parse(FileDepthAndTime[i][posTime]) - ts).ToString("HH:mm:ss dd.MM.yy");
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

        // метод сдвига колонки "замер"
        void UseShiftDepth()
        {
            int posZaboi = -1;
            int posZamer = -1;            
            bool isGoodShift = double.TryParse(txtBoxShift.Text, out double shiftValue);

            for (int i = 0; i < FileDepthAndTime[0].Length; i++)
            {
                if (FileDepthAndTime[0][i].Trim() == "Забой")
                {
                    posZaboi = i;                    
                }
                if (FileDepthAndTime[0][i].Trim() == "Замер")
                {
                    posZamer = i;
                }
            }

            if (!isGoodShift)
            {
                ScrollStatusLasTextBox("Значение смещения указано не верно. Пример: 3,5");
                return;
            }
            else if(posZamer == -1)
            {
                ScrollStatusLasTextBox("В таблице отсутсвует колонка \"Замер\"");
                return;
            }
            else if (posZaboi == -1)
            {
                ScrollStatusLasTextBox("В таблице отсутсвует колонка \"Забой\"");
                return;
            }

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                dataTable.Rows[i]["Замер"] = " " + (double.Parse(dataTable.Rows[i]["Забой"].ToString()) - shiftValue).ToString() + " ";
            }

            for (int i = 1; i < FileDepthAndTime.Count; i++)
            {
                FileDepthAndTime[i][posZamer] = (double.Parse(FileDepthAndTime[i][posZaboi]) - shiftValue).ToString();
            }

        }

       // кнопка обновить данные таблицы
        private void btn_UpdateCurrentData_Click(object sender, RoutedEventArgs e)
        {
            UseShiftTime_Click();
            UseShiftDepth();
        }

        private void btn_HeadLasWrite_Click(object sender, RoutedEventArgs e)
        {
            dataTab.Focus();
            lstBoxLasValues.Focus();
        }

        private void btn_BackToDataGrid_Click(object sender, RoutedEventArgs e)
        {
            filesTab.Focus();
        }
        
        // кнопка открытия калибровочного файла
        private void btn_OpenCalibrFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openCalibrFile = new OpenFileDialog();
            openCalibrFile.Filter = "Калибровочный файл|*.nk";
            openCalibrFile.Title = "Выберите подходящий калибровочный файл";
            // считываем данные из калибровочного файла
            if (openCalibrFile.ShowDialog() == true)
            {
                try
                {
                    // надо переделать выбор калибровочного файла
                    FileColibr = new List<string[]>();
                    using (var reader = new StreamReader(openCalibrFile.FileName, Encoding.GetEncoding(1251)))
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
                    StatusLasMenu += $"{DateTime.Now}: {ex.Message}\n";
                    return;
                }                
                ScrollStatusLasTextBox($"Калибровочный файл {openCalibrFile.SafeFileName} успешно считан");
                lblCalibrFile.Content = openCalibrFile.SafeFileName;
                IsOpenCalibFile = true;

                if (IsOpenCalibFile && IsOpenCalibFile)
                {
                    btnLasStart.IsEnabled = true;
                    txtBoxShift.IsEnabled = true;
                }
            }
            else
            {
                ScrollStatusLasTextBox("Необходимо выбрать калибровочный файл");                
            }

        }
    }
}
