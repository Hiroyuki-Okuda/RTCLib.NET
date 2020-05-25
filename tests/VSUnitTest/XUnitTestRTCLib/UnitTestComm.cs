using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using RTCLib;
using RTCLib.Comm;
using RTCLib.Sys;
using Xunit;

namespace XUnitTestRTCLib
{
    public class UnitTestComm
    {
        [StructLayout(layoutKind: LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
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

        [Fact(DisplayName = "Check UdpByteSender/Receiver")]
        public void BinarySendAndReceive()
        {
            Random cRandom = new System.Random();
            byte[] datatoSend = new byte[256];
            cRandom.NextBytes(datatoSend);

            RTCLib.Comm.UdpByteSender sender = new UdpByteSender("127.0.0.1", 30001);
            RTCLib.Comm.UdpByteReceiver receiver = new UdpByteReceiver(30001);

            byte[] receivedData = null;
            bool received = false;
            receiver.OnBytesReceived += bytes =>
            {
                receivedData = bytes;
                received = true;
            };

            // send
            sender.Send(datatoSend);

            // 1 sec wait
            for (int i = 0; i < 100; i++){ Thread.Sleep(10); if (received) break; }
            // equality check
            if (received)
                datatoSend.Is(receivedData);
            else
                Assert.True(false);

            // send
            received = false;
            sender.Send(datatoSend);
            datatoSend[0] = (byte)(datatoSend[0] + 1);
            // 1 sec wait
            for (int j = 0; j < 100; j++) { Thread.Sleep(10); if (received) break; }
            // equality check
            if (received)
                datatoSend.IsNot(receivedData);
            else
                Assert.True(false);
        }

        [Fact(DisplayName = "Check UdpBinarySender/Receiver")]
        public void StructureSendAndReceive()
        {
            TestSt toSendSt;
            toSendSt.intval = 99;
            toSendSt.floatval = 98.7f;
            toSendSt.mojiretsu = "mogemoge!";
            toSendSt.intarray = new int[4]{1, 2, 3, 4};


            using RTCLib.Comm.UdpBinarySender<TestSt> sender = new UdpBinarySender<TestSt>("127.0.0.1", 30001);
            using RTCLib.Comm.UdpBinaryReceiver<TestSt> receiver = new UdpBinaryReceiver<TestSt>();
            receiver.Open(30001);

            TestSt receivedData = default;
            bool received = false;
            receiver.OnDataReceived += dataReceived =>
            {
                receivedData = dataReceived;
                received = true;
            };

            // send 

            sender.Send(toSendSt);

            // 1 sec wait
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(10);
                if (received) break;
            }

            if (received)
            {
                // check contents
                toSendSt.IsStructuralEqual(receivedData);
            }
            else
            {
                Assert.True(false);
            }
            sender.Close();
            receiver.Close();
        }

        [Fact(DisplayName = "Check UdpBinaryTransceiver")]
        public void StructureSendAndReceiveTransceiver()
        {
            TestSt toSendSt;
            toSendSt.intval = 99;
            toSendSt.floatval = 98.7f;
            toSendSt.mojiretsu = "mogemoge!";
            toSendSt.intarray = new int[4] { 1, 2, 3, 4 };

            using UdpBinaryTransceiver<TestSt> sender = new UdpBinaryTransceiver<TestSt>
            ( "127.0.0.1", 30001, 30002);
            using UdpBinaryTransceiver<TestSt> receiver = new UdpBinaryTransceiver<TestSt>
                ("127.0.0.1", 30002, 30001);

            TestSt receivedData = default;
            bool received = false;
            receiver.OnDataReceived += dataReceived =>
            {
                receivedData = dataReceived;
                received = true;
            };

            // send 

            sender.Send(toSendSt);

            // 1 sec wait
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(10);
                if (received) break;
            }

            if (received)
            {
                // check contents
                toSendSt.IsStructuralEqual(receivedData);
            }
            else
            {
                Assert.True(false);
            }
            sender.Close();
            receiver.Close();

        }
    }
}
