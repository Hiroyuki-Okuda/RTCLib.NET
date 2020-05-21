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
        public Dictionary<string, string> param = new Dictionary<string, string>();

        int lcl_port;       /// local port

        //string remote_host; /// remote host to deliver parameter
        //int rmt_port;       /// remote port to deliver

        System.Net.Sockets.UdpClient udp;   /// udp client

        System.Net.IPEndPoint remoteEP;     /// endpoint

        string receive_buffer = "";

        /// <summary>
        /// async receive is alive
        /// </summary>
        public bool isAlive = false;

        /// <summary>
        /// for termination;
        /// </summary>
        public bool isTerminating = false;

        /// <summary>
        /// Open UDP
        /// </summary>
        /// <param name="remote_host">Remote IP</param>
        /// <param name="remote_port">Remote Port</param>
        /// <param name="local_port">Sending local port</param>
        /// <returns></returns>
        public int Open(int _local_port)
        {
            lcl_port = _local_port;

            // Create UDP client
            udp = new System.Net.Sockets.UdpClient(lcl_port);

            // Enable Broadcast
            udp.EnableBroadcast = true;

            // Set remote endpoint
            //remoteEP = new IPEndPoint(IPAddress.Any, 0);

            //udp.Connect(remoteEP);

            return 0;
        }

        public void StartReceiving()
        {
            receive_buffer = "";
            AsyncCallback callback = new AsyncCallback(ReceiveCallBack);
            udp.BeginReceive(callback, udp);
            isAlive = true;
            isTerminating = false;
        }

        public void StopReceiving()
        {
            isTerminating = true;
        }

        private void ReceiveCallBack(IAsyncResult res)
        {
            //if (!res.IsCompleted) return;
            byte[] data = udp.EndReceive(res, ref remoteEP);

            Encoding enc = Encoding.ASCII;
            string str = enc.GetString(data);
            receive_buffer = str;   // no concatenation now
            ParseString(receive_buffer);

            if (!isTerminating)
            {
                AsyncCallback callback = new AsyncCallback(ReceiveCallBack);
                udp.BeginReceive(callback, udp);
            }
            else {
                isAlive = false;
            }
        }

        private void ParseString(string str)
        {
            var x = str.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var y in x)
            {
                var z = y.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (z.Count() != 2) continue;
                param[z[0]] = z[1];
            }
        }

    }

}
