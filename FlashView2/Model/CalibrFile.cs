using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlashView2.Model
{
    public class CalibrFile : INotifyPropertyChanged
    {
        public List<string> spisokTrub;
        public List<string> SpisokTrub
        {
            get
            {
                return spisokTrub;
            }
            set
            {
                spisokTrub = value;
                OnPropertChanged("SpisokTrub");
            }
        } // Список труб для lb1_truba
        //public List<DateTime> DateCalibr { get; set; } // Даты калибровок
        public List<double[]> CoefsCalibr { get; set; } // Список коэффициентов калибровочного файла
        public List<string[]> TrubaZav { get; set; } // Тип математической зависимости
        public int CurrentChoise { get; set; } = -1;

        public CalibrFile()
        {
            SpisokTrub = new List<string>();
            //DateCalibr = new List<DateTime>();
            CoefsCalibr = new List<double[]>();
            TrubaZav = new List<string[]>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        // Проверка поддержки версии калибровочного файла
        public bool CheckCalibrVers(List<string> fileCalibrData)
        {
            string vers = fileCalibrData[0].Trim();
            if (int.TryParse(vers, out int versCalFile))
            {
                try
                {
                    switch (versCalFile)
                    {
                        case 3:
                            UpdateDataFileColibrVers3(fileCalibrData);
                            return true;
                        default:
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }

            }
            else
            {
                return false;
            }
        }

        // Обработка калибровочного файла. Извлечение полезных данных
        private void UpdateDataFileColibrVers3(List<string> fileCalibrData)
        {
            for (int i = 1; i < fileCalibrData.Count; i++)
            {
                string[] splitLine = fileCalibrData[i].Trim().Split(':');

                string[] lineCoefs = splitLine[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                double[] coefs = new double[lineCoefs.Length];
                for (int j = 0; j < lineCoefs.Length; j++)
                {
                    if (!double.TryParse(lineCoefs[j], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                    {
                        throw new ArgumentException("Ошибка в линии данных коэффициентов");
                    }
                    coefs[j] = value;
                }
                CoefsCalibr.Add(coefs);

                var truba = splitLine[1].Trim().Split(',')[0];
                SpisokTrub.Add(truba);

                var tempLine = splitLine[1].Trim().Split(',');
                var zavis = tempLine[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

                TrubaZav.Add(new string[] { truba, zavis });
                //if (!DateTime.TryParse(splitLine[3].Trim(), out DateTime time))
                //{
                //    throw new ArgumentException("Ошибка в линии данных времени калибровки");
                //}
                //DateCalibr.Add(time);
            }
            SpisokTrub = SpisokTrub.Distinct().ToList();
        }

        //private void UpdateDataFileColibrVers333(List<string> fileCalibrData)
        //{
        //    for (int i = 1; i < fileCalibrData.Count; i++)
        //    {
        //        string[] splitLine = fileCalibrData[i].Trim().Split(';');

        //        string[] lineCoefs = splitLine[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //        double[] coefs = new double[lineCoefs.Length];
        //        for (int j = 0; j < lineCoefs.Length; j++)
        //        {
        //            if (!double.TryParse(lineCoefs[j], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
        //            {
        //                throw new ArgumentException("Ошибка в линии данных коэффициентов");
        //            }
        //            coefs[j] = value;
        //        }
        //        CoefsCalibr.Add(coefs);

        //        SpisokTrub.Add(splitLine[1].Trim());
        //        TrubaZav.Add(new string[] { splitLine[1].Trim(), splitLine[2].Trim() });
        //        if (!DateTime.TryParse(splitLine[3].Trim(), out DateTime time))
        //        {
        //            throw new ArgumentException("Ошибка в линии данных времени калибровки");
        //        }
        //        DateCalibr.Add(time);
        //    }
        //    SpisokTrub = SpisokTrub.Distinct().ToList();
        //}
    }
}
