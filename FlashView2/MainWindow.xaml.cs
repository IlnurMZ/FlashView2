using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public byte[]? FlashFile { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItemOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Flash Files|*.fl";
            openFileDialog.Title = "Выберите flash-файл с данными";            

            if (openFileDialog.ShowDialog() == true)
            {
                string pathFlash = openFileDialog.FileName;
                try
                {
                    FlashFile = File.ReadAllBytes(pathFlash);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Возникла ошибка считывания файла. " + ex.Message);                    
                }
            }
            else
            {
                return;
            }
        }

        private void MenuItemCloseProgram_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
