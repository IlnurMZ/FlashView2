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
    }
}
