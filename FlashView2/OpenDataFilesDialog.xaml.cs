using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for OpenDataFilesDialog.xaml
    /// </summary>
    public partial class OpenDataFilesDialog : Window, INotifyPropertyChanged
    {
        //CalibrFile calibrFile;
        //public CalibrFile MyCalibrFile
        //{
        //    get
        //    {
        //        return calibrFile;
        //    }
        //    set
        //    {
        //        calibrFile = value;
        //        OnPropertyChanged("MyCalibrFile");
        //    }
        //}

        string? flashPath;
        public string? FlashPath
        {
            get
            {
                return flashPath;
            }
            set
            {
                flashPath = value;
                OnPropertyChanged("FlashPath");
            }
        }
        public ObservableCollection<string>? DepthPath { get; set; }

        public OpenDataFilesDialog()
        {
            InitializeComponent();            
            DepthPath = new ObservableCollection<string>();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        //кнопка открытия флеш файла
        private void btnOpenFlashFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Flash Files|*.fl";
            openFileDialog.Title = "Выберите flash-файл с данными";            

            if (openFileDialog.ShowDialog() == true)
            {
                FlashPath = openFileDialog.FileName;
            }            
        }

        // кнопка открытия файла с глубиной
        private void btnOpenDepthFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();           
            openFileDialog.Title = "Выберите файл с глубиной и временем";
            openFileDialog.Multiselect = true;                     
            
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    if (!DepthPath.Contains(file))
                    {
                        DepthPath?.Add(file);
                    }                                        
                }
            }
        }

        // кнопка удаления файла с глубиной
        private void btnDeletDepthFile_Click(object sender, RoutedEventArgs e)
        {
            if (listDepthFiles.SelectedItem == null) return;
            string selStr = listDepthFiles.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(selStr))
            {
                DepthPath.Remove(selStr);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            FlashPath = null;
            DepthPath = null;
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (DepthPath.Count == 0) DepthPath = null;
            if (string.IsNullOrEmpty(FlashPath))
            {
                MessageBox.Show("Необходимо выбрать данные flash");
            }
            else
            {
                Close();
            }            
        }

        //private void btnOpenCalibrFile_Click(object sender, RoutedEventArgs e)
        //{
        //    List<string> FileColibrData = new List<string>();
        //    OpenFileDialog openCalibrFile = new OpenFileDialog();
        //    openCalibrFile.Filter = "Калибровочный файл|*.nk";
        //    openCalibrFile.Title = "Выберите подходящий калибровочный файл";
        //    // считываем данные из калибровочного файла
        //    if (openCalibrFile.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        //            // надо переделать выбор калибровочного файла                    
        //            using (var reader = new StreamReader(openCalibrFile.FileName, Encoding.GetEncoding(1251)))
        //            {
        //                while (!reader.EndOfStream)
        //                {
        //                    string line = reader.ReadLine();
        //                    if (!string.IsNullOrEmpty(line))
        //                    {                             
        //                        FileColibrData.Add(line);                               
        //                    }
        //                }
        //            }
        //            MyCalibrFile = new CalibrFile();
        //            bool isSupportedVers = MyCalibrFile.CheckCalibrVers(FileColibrData);
        //            if (!isSupportedVers)
        //            {
        //                MessageBox.Show("Текущий калибровочный файл не поддерживается");                        
        //                return;
        //            }
        //        }
        //        catch
        //        {
        //            MessageBox.Show($"Произошла ошибка чтения файла");                    
        //            return;
        //        }                
        //        txtBlCalibr.Text = openCalibrFile.FileName;                
        //    }            
        //}
    }
}
