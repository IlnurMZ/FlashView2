using System;
using System.Data;
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
using DocumentFormat.OpenXml.Drawing.Charts;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlashZTK_I
{
    /// <summary>
    /// Interaction logic for FIlter.xaml
    /// </summary>
    public partial class FIlterWindow : Window, INotifyPropertyChanged
    {        
        public List<(DateTime?, DateTime?)> Periods { get; set; }
        private System.Data.DataTable myDataTable;

        public bool IsDepthFile { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        public FIlterWindow(System.Data.DataTable myDataTable, List<(DateTime?, DateTime?)> periods, bool isDepth)
        {
            InitializeComponent();
            this.myDataTable = myDataTable;
            Periods = periods;
            DataContext = this;
            IsDepthFile = isDepth;
            MessageBox.Show("Данный пункт меню пока только ознакомительный");
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            string sqlQuerry = "";
            bool IsPeriod = cbFilterByPeriod.IsChecked.Value;
            bool IsStat = cbFilterByStat.IsChecked.Value;
            if (IsPeriod)
            {
                sqlQuerry += string.Format("Дата >= '{0}' AND Дата <= '{1}'",
                Periods[lbPeriods.SelectedIndex].Item1.Value.ToString("HH:mm:ss dd/MM/yyyy"),
                Periods[lbPeriods.SelectedIndex].Item2.Value.ToString("HH:mm:ss dd/MM/yyyy"));
            }            
            if (IsStat && IsPeriod)
            {
                sqlQuerry += " AND СОСТОЯНИЕ = 3";
            }
            else if (IsStat)
            {
                sqlQuerry += "СОСТОЯНИЕ = 3";
            }
            else if (!IsPeriod && !IsStat)
            {
                sqlQuerry = "";
            }
            myDataTable.DefaultView.RowFilter = sqlQuerry;
            Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
