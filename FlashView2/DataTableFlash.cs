using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FlashView2
{
    internal class DataTableFlash 
    {
        public DataTable Table { get; set; }        
        public DataTableFlash(List<string> headerColumns)
        {
            Table = new DataTable();
            
            foreach (var column in headerColumns)
            {                
                Table.Columns.Add(column);
            }
        }
    }
}
