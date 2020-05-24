using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Xunit;

using RTCLib;
using RTCLib.Sys;

namespace XUnitTestRTCLib
{
    public class UnitTestSys
    {
        [StructLayout(layoutKind:LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        struct TestSt
        {
            //! simple int
            public int intval;

            //! simple float 
            public float floatval;

            //! array data
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] intarray;

            //! string data
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string mojiretsu;
        }

        [Fact(DisplayName = "Check structure to bytes")]
        public void Interop_St2Bytes()
        {
            TestSt origin = new TestSt();
            origin.floatval = 1.0f;
            origin.intval = 1;
            origin.intarray = new[] {1, 2, 3, 4};
            origin.mojiretsu = "hoge!";

            byte[] data = Interop.StructureToBytes(origin);
            TestSt copy = Interop.BytesToStructure<TestSt>(data);

            origin.IsStructuralEqual(copy);
        }
    }
}
