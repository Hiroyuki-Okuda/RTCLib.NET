using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

using RTCLib;
using RTCLib.Fio;
using RTCLib.Sys;

namespace XUnitTestRTCLib
{
    public class UnitTestFio
    {

        [Fact(DisplayName = "CsvReader")]
        public void TestCsvReader()
        {
            // make csv strings
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("x, y, z, ");
            sb.AppendLine("10,11,12,");
            sb.AppendLine("20,21,22,");
            sb.AppendLine("30,31,32,");
            sb.AppendLine(",");
            sb.AppendLine("");

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms, Encoding.Unicode);
            string textorg = sb.ToString();
            sw.Write(textorg);
            sw.Flush();

            ms.Seek(0, SeekOrigin.Begin);
            StreamReader sr = new StreamReader(ms, Encoding.Unicode);
            string text = sr.ReadToEnd();
            sr.BaseStream.Seek(0, SeekOrigin.Begin);

            CsvReader csvReader = new CsvReader();
            csvReader.Open(sr, 1, Encoding.Unicode);

            // Check values
            csvReader.Cols().Is(3);
            csvReader.Rows.Is(3);
            csvReader[0][0].Is(10);
            csvReader[1][1].Is(21);
            csvReader[2][2].Is(32);
        }
    }
}
