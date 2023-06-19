using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace FlashZTK_I
{
    /// <summary>
    /// Interaction logic for OpenDataFilesDialog.xaml
    /// </summary>
    public partial class OpenDataFilesDialog : Window, INotifyPropertyChanged
    {
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

        private void btnOpenDepthFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файл глубина-время|*.txt";
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

        private void btnDeletDepthFile_Click(object sender, RoutedEventArgs e)
        {
            var selStr = listDepthFiles.SelectedItem.ToString();
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
    }
}
