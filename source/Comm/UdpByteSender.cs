using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RTCLib
{
    public class UdpByteSender : IDisposable
    {
        // Local port to open Udp
        // int _localPort; // < disabled local wait

        // Remote host information
        private string _remoteHost;
        private int _remotePort;

        // udpClient client
        System.Net.Sockets.UdpClient _udpClient;

        // send target
        System.Net.IPEndPoint _remoteEndPoint;

        // event executed on data send completed
        public event Action SendCompleted;

        public UdpByteSender()
        {
            _remoteEndPoint = null;
        }

        public UdpByteSender(string remoteHost, int remotePort)
        {
            _ = Open(remoteHost, remotePort);
        }

        private void CreateUdpClient()
        {
            _udpClient = new UdpClient();
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            _ = _udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
        }

        /// <summary>
        /// UDP通信を開く．送信する．
        /// </summary>
        /// <param name="remoteHost">送信先IP</param>
        /// <param name="remotePort">送信先ポート</param>
        /// <returns></returns>
        public int Open( string remoteHost, int remotePort ) {
            _remoteHost = remoteHost;
            _remotePort = remotePort;
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(_remoteHost), _remotePort);

            CreateUdpClient();
            return 0;
        }

        public int Send(byte[] messageBytes)
        {
            var ret = Send(messageBytes, _remoteEndPoint);
            return ret;
        }

        public int Send(byte[] messageBytes, IPEndPoint sendTo)
        {
            int ret = 0;
            if (messageBytes != null)
            {
                ret = _udpClient.Send(messageBytes, messageBytes.Length, sendTo);
            }
            OnSendCompleted();
            return ret;
        }

        public async Task<int> SendAsync(byte[] messageBytes)
        {
            var ret = SendAsync(messageBytes, _remoteEndPoint);
            return await ret.ConfigureAwait(false);
        }

        public async Task<int> SendAsync(byte[] messageBytes, IPEndPoint sendTo)
        {
            Task<int> ret = null;
            if (messageBytes != null)
            {
                ret = _udpClient.SendAsync(messageBytes, messageBytes.Length);
            }
            OnSendCompleted();
            return await ret;
        }

        protected virtual void OnSendCompleted()
        {
            SendCompleted?.Invoke();
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
        }
    }
}