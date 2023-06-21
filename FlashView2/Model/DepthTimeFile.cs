using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Presentation;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace FlashZTK_I.Model
{
    public class DepthTimeFile
    {
        public string DepthName { get; set; }
        public string TimeName { get; set; }
        public string DateName { get; set; }
        public string StatusName { get; set; }
        public int lengthStr { get; set; }
        public string? Year { get; set; }
        public char Separator { get; set; }
        public DateTime? PrevDate { get; set; }
        public int[] ColNumbers { get; set; }
        //ColNumbers[0] - индекс столбца "время"
        //ColNumbers[1] - индекс столбца "дата"
        //ColNumbers[2] - индекс столбца "забой"
        //ColNumbers[3] - индекс столбца "sbSendData сост."

        public List<double> DepthList { get; set; }
        public List<DateTime> DateList { get; set; }
        public List<bool> StatusList { get; set; }

        public DepthTimeFile()
        {
            // значения по умолчанию для старой версии файла с глубиной и временем
            DepthName = "забой,[м.]";
            TimeName = "время";
            DateName = "дата";
            StatusName = "sbSendData";
            ColNumbers = new int[4] { -1, -1, -1, -1 };
            PrevDate = null;
            Separator = ' ';

            DepthList = new List<double>();
            DateList = new List<DateTime>();
            StatusList = new List<bool>();
        }
    }
}
