using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTCLib.Comm
{
    /// <summary>
    /// 
    /// </summary>
    public class UdpParameterReceiver
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Param = new Dictionary<string, string>();

        int _lclPort;       /// local port

        //string remote_host; /// remote host to deliver parameter
        //int rmt_port;       /// remote port to deliver

        System.Net.Sockets.UdpClient _udp;   /// udp client

        System.Net.IPEndPoint _remoteEp;     /// endpoint

        string _receiveBuffer = "";

        /// <summary>
        /// async receive is alive
        /// </summary>
        public bool IsAlive = false;

        /// <summary>
        /// for termination;
        /// </summary>
        public bool IsTerminating = false;

        /// <summary>
        /// Open UDP
        /// </summary>
        /// <param name="localPort">Sending local port</param>
        /// <returns></returns>
        public int Open(int localPort)
        {
            _lclPort = localPort;

            // Create UDP client
            _udp = new System.Net.Sockets.UdpClient(_lclPort);

            // Enable Broadcast
            _udp.EnableBroadcast = true;

            // Set remote endpoint
            //remoteEP = new IPEndPoint(IPAddress.Any, 0);

            //udp.Connect(remoteEP);

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartReceiving()
        {
            _receiveBuffer = "";
            AsyncCallback callback = new AsyncCallback(ReceiveCallBack);
            _udp.BeginReceive(callback, _udp);
            IsAlive = true;
            IsTerminating = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopReceiving()
        {
            IsTerminating = true;
        }

        private void ReceiveCallBack(IAsyncResult res)
        {
            //if (!res.IsCompleted) return;
            byte[] data = _udp.EndReceive(res, ref _remoteEp);

            Encoding enc = Encoding.ASCII;
            string str = enc.GetString(data);
            _receiveBuffer = str;   // no concatenation now
            ParseString(_receiveBuffer);

            if (!IsTerminating)
            {
                AsyncCallback callback = new AsyncCallback(ReceiveCallBack);
                _udp.BeginReceive(callback, _udp);
            }
            else {
                IsAlive = false;
            }
        }

        private void ParseString(string str)
        {
            var x = str.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var y in x)
            {
                var z = y.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (z.Length != 2) continue;
                Param[z[0]] = z[1];
            }
        }

    }

}
