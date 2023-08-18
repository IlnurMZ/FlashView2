using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace FlashView2.Model
{
    public class DepthTimeLas
    {
        public delegate void AddData(string row);
        public AddData addData;        
        public string? Year { get; set; } // год, который указывается в шапке файла с данными
        public char Separator { get; set; } // разделитель данных
        public DateTime? PrevDate { get; set; } // дата, необх. для отслеживания изменения года       
        public int ColumnZab; // номер столбца с забоем
        public int ColumnDate; // номер столбца с датой
        public int ColumnStat; // номер столбца с датой
        public List<string[]> Data { get; set; } // данные считанные с файла глубиномера
        public List<string> ColNames; // названия столбцов

        public DepthTimeLas()
        {            
            PrevDate = null;
            Separator = ' ';           
            Data = new List<string[]>();
            ColNames = new List<string>();
        }
       
        public void FillStandColumn()
        {
            ColNames.Add("дата");
            ColNames.Add("мс");
            ColNames.Add("забой");
            ColNames.Add("sbSendData акт.");
            ColNames.Add("sbSendData сост.");

            ColumnZab = 2;
            ColumnDate = 0;
        }

        public void LoadDepthFromGlub(string row )
        {              
            if (ColNames.Count > 0)
            {
                return;
            }
            var strSplit = row.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < strSplit.Length - 1; i++)
            {
                if (strSplit[i].Trim() == "дата" && strSplit[i + 1].Trim() == "время")
                {
                    ColumnDate = i;
                    break;
                }
            }

            string[] tempAr = new string[strSplit.Length - 1];
            strSplit[ColumnDate + 1] = "дата";
            Array.Copy(strSplit, 0, tempAr, 0, ColumnDate);
            Array.Copy(strSplit, ColumnDate + 1, tempAr, ColumnDate, strSplit.Length - ColumnDate - 1);

            int lengthStr = tempAr.Length;            
            
            for (int i = 0; i < lengthStr - 4; i++)
            {
                
                foreach (var symb in tempAr[i])
                {
                    tempAr[i] = tempAr[i].Replace("[", "").Replace("]","").TrimEnd('.').TrimStart('/');
                }
                ColNames.Add(tempAr[i]);                
            }           
            ColNames.Add(tempAr[lengthStr - 4] + " " + tempAr[lengthStr - 3]);
            ColNames.Add(tempAr[lengthStr - 2] + " " + tempAr[lengthStr - 1]);                       
            addData = AddGlubRow;
        }

        public void LoadDepthFromZtkProg(string row)
        {
            if (ColNames.Count > 0)
            {
                return;
            }
                    
            var tempArray = row.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tempArray.Length - 1; i++)
            {
                if (tempArray[i].Trim() == "Время" && tempArray[i + 1].Trim() == "Дата")
                {
                    ColumnDate = i + 1;
                    break;
                }
            }
            tempArray[ColumnDate - 1] = "Дата";
            ColNames.AddRange(tempArray.SkipLast(1).Select(x => x.Trim()).ToArray());                       
            addData = AddZtkProgRow;
        }

        public void AddGlubRow(string row) 
        {
            string[] array = row.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            bool isTableData = DateTime.TryParse(array[ColumnDate] + " " + array[ColumnDate + 1], out DateTime date);
            if (isTableData)
            {
                string[] tempAr = new string[array.Length - 1];
                // дата + время, находятся в разных столбцах
                array[ColumnDate + 1] = array[ColumnDate] + " " + array[ColumnDate + 1];
                Array.Copy(array, 0, tempAr, 0, ColumnDate);
                Array.Copy(array, ColumnDate + 1, tempAr, ColumnDate, array.Length - ColumnDate - 1);
                Data.Add(tempAr);
            }            
        }

        public void AddZtkProgRow(string row)
        {
            string[] array = row.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                                                                                                                              
            bool isTableData = int.TryParse(array[0], out int temRes);
            if (isTableData)
            {
                DateTime date = DateTime.Parse(array[ColumnDate - 1] + ":00 " + array[ColumnDate] + "." + Year);
                if (PrevDate == null)
                {
                    PrevDate = date;
                }
                else
                {
                    if (PrevDate > date)
                    {
                        Year = (int.Parse(Year) + 1).ToString();
                        date = date.AddYears(1);
                        PrevDate = date;
                    }
                }

                array[ColumnDate - 1] = $"{date}";
                if (array.Length == ColNames.Count + 1)
                {
                    Data.Add(array.SkipLast(1).ToArray());
                }
            }
        }
    }
}
