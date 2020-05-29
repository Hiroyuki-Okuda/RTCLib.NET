using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

using RTCLib;
using RTCLib.Sys;
using Xunit.Abstractions;

namespace XUnitTestRTCLib
{
    public class UnitTestSys
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTestSys(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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

        [Fact(DisplayName = "Check Logger work")]
        public void MessageLoggerTest()
        {
            int lowCount = 0;
            int midCount = 0;
            int highCount = 0;
            using (MessageLogger logger = new MessageLogger())
            {
                logger.AddLogDevice(
                    MessageLogger.LoggingLevel.Low,
                    null,
                    (device, message) =>
                    {
                        _testOutputHelper.WriteLine("Device Low:" + message);
                        Thread.Sleep(100); // device latency
                        lowCount++;
                    });
                logger.AddLogDevice(
                    MessageLogger.LoggingLevel.Middle,
                    null,
                    (device, message) =>
                    {
                        _testOutputHelper.WriteLine("Device Mid:" + message);
                        Thread.Sleep(100); // device latency
                    midCount++;
                    });
                logger.AddLogDevice(
                    MessageLogger.LoggingLevel.High,
                    null,
                    (device, message) =>
                    {
                        _testOutputHelper.WriteLine("Device High:" + message);
                        Thread.Sleep(100); // device latency
                    highCount++;
                    });
                logger.StartLog(10);

                for (int i = (int)MessageLogger.LoggingLevel.Lowest - 25; i < (int)MessageLogger.LoggingLevel.Highest + 25; i += 50)
                {
                    Thread.Sleep(100);
                    // Only message i=>5 should be output
                    logger.AddLog($"Hoge {i}!", (MessageLogger.LoggingLevel)i);
                }

                logger.WaitForLogComplete();
                lowCount.Is(6);  // here may be failed because of the latency
                midCount.Is(4);  
                highCount.Is(2); // use ProcessMessage to wait accomplishment 
            }

            // followings must success after closing devices
            // wait for all messages completion when logger is disposed. 
            lowCount.Is(6);
            midCount.Is(4);
            highCount.Is(2);

        }
    }
}
