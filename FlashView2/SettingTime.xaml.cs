using System;
using System.Collections.Generic;
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

namespace FlashView2
{
    /// <summary>
    /// Interaction logic for SettingTime.xaml
    /// </summary>
    public partial class SettingTime : Window, INotifyPropertyChanged
    {
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
        }

        //bool isMove;
        //public bool IsMove
        //{
        //    get
        //    {
        //        return isMove;
        //    }
        //    set
        //    {
        //        isMove = value;
        //        OnPropertyChanged("IsMove");
        //    }
        //}

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
        }
        public SettingTime()
        {
            InitializeComponent();
            ShiftTime = "00:00:00";
            DataContext = this;
            IsMoveTimeUp = true;
            //IsMove = true;
            
        }

        bool isShift = false;
        public bool IsShift
        {
            get
            {
                return isShift;
            }
            set
            {
                isShift = value;
            }
        }

        private void btnOK_SettingTime_Click(object sender, RoutedEventArgs e)
        {
            IsShift = true;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        private void btnCancel_SettingTime_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
