using System;
using System.Collections.Generic;
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

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for LasMenuForm.xaml
    /// </summary>
    public partial class LasMenuForm : Window
    {
        public string DateA { get; set; }
        public string DateB { get; set; }
        public string DiamOfTrub { get; set; }
        public bool isLin { get; set; }
        public LasMenuForm()
        {
            DateA = DateTime.Now.ToString();
            DateB = DateTime.Now.AddHours(2).ToString();
            InitializeComponent();
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {            
            DiamOfTrub = lb1_truba.Text;
            isLin = rb1_Lin.IsChecked.Value;
            this.DialogResult = true;
        }
    }
}
