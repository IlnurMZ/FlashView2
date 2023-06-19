using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashView2
{
    public class Packet
    {
        // данные для строк
        public byte ID_Packet { get; set; } // ID пакета
        public byte ID_Device { get; set; } // ID устройства
        public List<byte> LengthParams { get; set; } // количество байт на параметр
        public List<string> TypeParams { get; set; } // тип параметра        
        public List<string> TypeCalculate { get; set; } // тип вычисления
        public List<double[]> DataCalculation { get; set; } // данные для вычисления
        public List<byte> CountSign { get; set; } // количество знаков после запятой        
        public byte[] endLine { get; set; } // 2 байта на конец строки

        // данные для таблицы
        public List<string> HeaderColumns { get; set; } // заголовки в таблице
        public List<byte> WidthColumn { get; set; } // ширина столбца
        public int LengthLine { get; set; } // длина строки в таблице
        public double badValue { get; set; } // пока не учавствует в работе


        public Packet()
        {
            LengthParams = new List<byte>();
            TypeParams = new List<string>();
            HeaderColumns = new List<string>();
            TypeCalculate = new List<string>();
            DataCalculation = new List<double[]>();
            CountSign = new List<byte>();
            WidthColumn = new List<byte>();
            endLine = new byte[2];
        }
        // вычисление значения по его типу
        public string CalculateValueByType(string typeCalc, string value, double[] data, byte countSign)
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
                        if (countSign > 0)
                        {
                            result = Math.Round((data[0] * doubleValue + data[1]), countSign).ToString();
                        }
                        else
                        {
                            result = (data[0] * doubleValue + data[1]).ToString();
                        }
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

        // получение значения по типу
        public string GetValueByType(string typeValue, byte[] value)
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

        // метод преобразования пачки байтов в дату
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
            if (!DateTime.TryParse(date, out DateTime timeTest))
            {
                throw new FormatException("Ошибка формата времени");
            }
            return date;
        }
    }
}
