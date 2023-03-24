using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashView2
{
    public class ConfFileInfo
    {
        public string PathFile { get; set; }
        public byte[] StartBytes { get; set; }
        public byte[] EndBytes { get; set; }
        public byte LengthLine { get; set; }       

        public ConfFileInfo(string pathFile, byte[] startBytes, byte[] endBytes, byte lengthLine)
        {
            PathFile = pathFile;
            StartBytes = startBytes;
            EndBytes = endBytes;
            LengthLine = lengthLine;
        }
    }
}
