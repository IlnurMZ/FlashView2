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
using System.Windows.Shapes;
using Microsoft.Win32;

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for LasMenuForm.xaml
    /// </summary>
    public partial class LasMenuForm : Window
    {
        public string StartTimeRead { get; set; } // Стартовое время считывания
        public string EndTimeRead { get; set; } // Конечное время считывания
        public string DiamOfTrub { get; set; } // диаметр трубы
        public bool isLineCalc { get; set; } // тип расчета Кп (линейный или квардратичный)
        public List<string> FileDepthAndTime { get; set; }
        public LasMenuForm()
        {
            StartTimeRead = DateTime.Now.ToString();
            EndTimeRead = DateTime.Now.AddHours(2).ToString();
            InitializeComponent();
            DataContext = this;
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

            if (openFileDialog.ShowDialog()==true)
            {
                path = openFileDialog.FileName;
                try
                {
                    using (var reader = new StreamReader(path))
                    {
                        while (!reader.EndOfStream)
                        {
                            var row1 = reader.ReadLine();                            
                            if (!string.IsNullOrEmpty(row1))
                            {
                                if (row1.Contains('|') && row1.Contains("Забой"))
                                {
                                    
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
        }
    }
}
