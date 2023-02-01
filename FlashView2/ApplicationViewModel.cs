using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

namespace FlashView2
{
    public class ApplicationViewModel : INotifyPropertyChanged
    {
        //public DataTableFlash? DataTableFlash { get; set; }
        private DataTable dataTable;        
        public DataTable DataTable 
        {
            get
            {
                return dataTable;
            }
            set
            {
                dataTable = value;
                OnPropertyChanged("DataTable");
            } 
        }
        public byte ID_Device { get; set; }
        public byte ID_Packet { get; set; }
        public List<Packet> Packets { get; set; }        
        public ApplicationViewModel(byte[] flash, List<Packet> packets)
        {
            Packets = packets;
            ID_Device = flash[1];
            ID_Packet = flash[0];            
            DataTable = LoadDataTable(packets, flash);           
        }

        string CalculateValueByType(string typeCalc, string value, double[] data) // по типу вычисления выдаем результат
        {
            string result = "";
            switch (typeCalc) // смотрим тип вычисления
            {
                case "нет":
                case "вр":
                    result = value;
                    break;
                case "лин":
                    if (double.TryParse(value, out double doubleValue))
                    {
                        result = (data[0] * doubleValue + data[1]).ToString();
                    }
                    else
                    {
                        throw new Exception("не удалось преобразовать данное при лин.вычислении");
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        string GetValueByType(string typeValue, byte[] value)
        {
            string result = "";
            switch (typeValue)
            {
                case "byteS":
                    result = ((sbyte)(value[0])).ToString();
                    break;
                case "byteUs":
                    result = ((byte)(value[0])).ToString();
                    break;
                case "shortS":
                    result = ((short)(value[0] | (value[1] << 8))).ToString();
                    break;
                case "shortUs":
                    result = ((ushort)(value[0] | (value[1] << 8))).ToString();
                    break;
                //case "intS":
                //    break;
                //case "intUs":
                //    break;
                case "bdTime":
                    result = GetbdTime(value);
                    break;
                default:
                    throw new FormatException("неизвестный тип данных");
            }
            return result;
        }

        string GetbdTime(Byte[] array)
        {
            string dateTime = Convert.ToHexString(array);
            string second = "";
            string minute = "";
            string hour = "";
            string day = "";
            string month = "";
            string year = "";
            string date;

            second = dateTime[0].ToString() + dateTime[1].ToString();
            minute = dateTime[2].ToString() + dateTime[3].ToString();
            hour = dateTime[4].ToString() + dateTime[5].ToString();
            day = dateTime[6].ToString() + dateTime[7].ToString();
            month = dateTime[8].ToString() + dateTime[9].ToString();
            year = dateTime[10].ToString() + dateTime[11].ToString();
            //date = new DateTime(year, month, day, hour, minute, second);
            date = $"{hour}:{minute}:{second} {day}.{month}.{year}";

            return date;
        }

        DataTable LoadDataTable(List<Packet> PacketsSettings, byte[] FlashFile)
        {            
            DataTable dt = new DataTable();          

            //for (int i = 0; i < PacketsSettings[0].HeaderColumns.Count; i++)
            //{
            //    dt.Columns.Add();
            //}

            foreach (var item in PacketsSettings[0].HeaderColumns)
            {
                dt.Columns.Add(item);
            }

            int countColumn = PacketsSettings[0].HeaderColumns.Count;        

            // вычисляем изначальные id пакета и устройства
            byte idPacketArray = FlashFile[0];
            byte idDeviceArray = FlashFile[1];
            // выбираем нужный пакет исходя из id-шников 
            var myPacket = PacketsSettings[0];
            byte[] endLinePacket = myPacket.endLine;
            int countByteRow = myPacket.LengthLine; // количество байт на строку
            byte countParams = (byte)myPacket.TypeParams.Count; // количество столбцов
            int countBadByte = 0;
            DataRow row;
            byte loadStatus = 0;
            byte tempVal = 0;

            for (int i = 0; i < 100; i++) // FlashFile.Length; i++)
            {
                // условие захода в начало строки
                bool isGoodStartLine = FlashFile[i] == idPacketArray && FlashFile[i + 1] == idDeviceArray;

                if (i + countByteRow > FlashFile.Length) // проверка завершенности строки, чтобы исключить выход за пределы массива байт
                {
                    break;
                }
                // проверка двух байт на конец строки
                bool isGoodEndLine = FlashFile[i + countByteRow - 2] == endLinePacket[0] && FlashFile[i + countByteRow - 1] == endLinePacket[1];

                if (isGoodStartLine && isGoodEndLine) // проверка совпадения на начало строки
                {
                    row = dt.NewRow(); // создаем строку для таблицы
                    for (int j = 0; j < countParams; j++)
                    {
                        byte countByte = myPacket.LengthParams[j]; // определяем количество байт на параметр
                        byte[] values = new byte[countByte]; // берем необходимое количество байт                   
                        Array.Copy(FlashFile, i, values, 0, countByte); // копируем наш кусок
                        string valueA = GetValueByType(myPacket.TypeParams[j], values); // вычисляем значение по типу данных
                        string valueB = CalculateValueByType(myPacket.TypeCalculate[j], valueA, myPacket.DataCalculation[j]); // вычисляем пересчет данного по типу
                        row[j] = valueB;
                        i += countByte; // смещаем курсор по общему массиву байт                        
                    }
                    i--;
                    dt.Rows.Add(row);                    
                }
                else
                {
                    countBadByte++;
                }

                //tempVal = (byte)(i * 1.0 / FlashFile.Length * 100);
                //if (tempVal >= loadStatus)
                //{
                //    loadStatus = (byte)(tempVal + 10);
                //    Dispatcher.Invoke(() =>
                //    {
                //        //var progress = new Progress<int>(value => progBar.Value = value);
                //        //((IProgress<int>)progress).Report(loadStatus);
                //        progBar.Value = loadStatus;
                //    });
                //}
            }
            return dt;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
