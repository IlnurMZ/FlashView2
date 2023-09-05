using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using FlashView2.Model;
using Microsoft.Win32;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window, INotifyPropertyChanged
    {
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

        bool isAddColumn;
        public bool IsAddColumn
        {
            get
            {
                return isAddColumn;
            }
            set
            {
                isAddColumn = value;
                OnPropertyChanged("IsAddColumn");
            }
        }

        string path;
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                OnPropertyChanged("Path");
            }
        }
        public SettingWindow(CalibrFile calibrFile)
        {            
            InitializeComponent();
            DataContext = this;
            MyCalibrFile = calibrFile;
            Path = MyCalibrFile.Path;
            if (string.IsNullOrEmpty(Path)) btnOK.IsEnabled = false;
            if (MyCalibrFile.CurrentChoise != -1)
            {
                int selectInd = MyCalibrFile.CurrentChoise % 3;
                lb1_truba.SelectedIndex = selectInd;
                if (MyCalibrFile.TrubaZav[MyCalibrFile.CurrentChoise][1] == "линейная")
                {
                    rb1_Lin.IsChecked = true;
                }
                else
                {
                    rb2_Kvad.IsChecked = true;
                }
            }
            IsAddColumn = MyCalibrFile.IsAddColum;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {      
            string diamTruba = lb1_truba.Text;//SelectedItem.ToString();
            string typeOfCalc;
            bool isLineCalc = rb1_Lin.IsChecked == true ? true : false;
            if (isLineCalc)
            {
                typeOfCalc = "линейная";
            }
            else
            {
                typeOfCalc = "квадратичная";
            }

            for (int i = 0; i < MyCalibrFile.TrubaZav.Count; i++)
            {
                if (MyCalibrFile.TrubaZav[i][0].Contains(diamTruba) && MyCalibrFile.TrubaZav[i][1].Contains(typeOfCalc))
                {
                    if (MyCalibrFile.CurrentChoise != i)
                    {
                        MyCalibrFile.IsChangedCalc = true;
                    }
                    else
                    {
                        MyCalibrFile.IsChangedCalc = false;
                    }
                    MyCalibrFile.CurrentChoise = i;
                    break;
                }
            }

            if (MyCalibrFile.IsAddColum == false && IsAddColumn == true) MyCalibrFile.IsChangedCalc = true;

            MyCalibrFile.IsAddColum = IsAddColumn;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOpenCalibrFile_Click(object sender, RoutedEventArgs e)
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
                                FileColibrData.Add(line);
                            }
                        }
                    }
                    //MyCalibrFile = new CalibrFile();
                    bool isSupportedVers = MyCalibrFile.CheckCalibrVers(FileColibrData);
                    if (!isSupportedVers)
                    {
                        MessageBox.Show("Текущий калибровочный файл не поддерживается");
                        return;
                    }
                    MyCalibrFile.Path = openCalibrFile.FileName;
                    MyCalibrFile.IsChangedCalc = true;
                }
                catch
                {
                    MessageBox.Show($"Произошла ошибка чтения файла");
                    return;
                }
                txtBlCalibr.Text = MyCalibrFile.Path;
                btnOK.IsEnabled = true;
            }
        }
    }
}
