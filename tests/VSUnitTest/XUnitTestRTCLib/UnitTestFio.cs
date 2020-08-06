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

        class Test01
        {
            public int A = 9;
            public int[] B = new []{10,11,12};
            public Test02 D = new Test02();
            public Test02[] E = new Test02[2]
            {
                new Test02(){ G = 100, H = new int[]{101,102,103}, I = "s100" },
                new Test02(){ G = 200, H = new int[]{201,202,203}, I = "s200" }
            };
        }
        class Test02
        {
            public int G = 50;
            public int[] H = new[] { 51, 52, 53};
            public string I = "s000";
        }

        [Fact(DisplayName = "CsvWriterTag")]
        public void TestCsvWriterTag()
        {
            Test02 target = new Test02();
            CsvWriter<Test02> writer = new CsvWriter<Test02>();

            writer.RegField(o=>o.G, "G");
            writer.RegFieldArray(o => o.H, target.H.Length, "H_#");
            writer.RegField(o => o.I, "I");
            string tag02 = writer.GetHeaderString();

            tag02.Is("G,H_0,H_1,H_2,I");

        }

        private void RegistClassTest02(CsvWriter<Test02> writer)
        {
            writer.RegField(o => o.G, "G");
            writer.RegFieldArray(o => o.H, 3, "H_#");
            writer.RegField(o => o.I, "I");
        }

        [Fact(DisplayName = "CsvWriterTagNested")]
        public void TestCsvWriterTagNested()
        {
            Test01 target = new Test01();
            CsvWriter<Test01> writer = new CsvWriter<Test01>();

            writer.RegField(o => o.A, "A");
            writer.RegFieldArray(o => o.B, target.B.Length, "B_#");
            writer.RegFieldClass(RegistClassTest02, o => o.D, "D");
            writer.RegFieldClassArray(RegistClassTest02, o => o.E, target.E.Length, "E_#");

            string tag02 = writer.GetHeaderString();

            string ideal = "A,B_0,B_1,B_2,";
            ideal += "D.G,D.H_0,D.H_1,D.H_2,D.I,";
            ideal += "E_0.G,E_0.H_0,E_0.H_1,E_0.H_2,E_0.I,";
            ideal += "E_1.G,E_1.H_0,E_1.H_1,E_1.H_2,E_1.I";

            tag02.Is(ideal);

        }

        [Fact(DisplayName = "CsvWriterValues")]
        public void TestCsvWriterValues()
        {
            Test01 target = new Test01();
            CsvWriter<Test01> writer = new CsvWriter<Test01>();

            writer.RegField(o => o.A, "A");
            writer.RegFieldArray(o => o.B, target.B.Length, "B_#");
            writer.RegFieldClass(RegistClassTest02, o => o.D, "D");
            writer.RegFieldClassArray(RegistClassTest02, o => o.E, target.E.Length, "E_#");

            string body = writer.GetOneRowString(target);

            string ideal = "9,10,11,12,";
            ideal += "50,51,52,53,s000,";
            ideal += "100,101,102,103,s100,";
            ideal += "200,201,202,203,s200";

            body.Is(ideal);

        }
    }
}
