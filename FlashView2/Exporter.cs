using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop.Excel;

namespace FlashView2
{
    public class Exporter
    {
        System.Data.DataTable dataTable;
        public Exporter(System.Data.DataTable dt)
        {
            dataTable = dt;
        }
        
    }
}
