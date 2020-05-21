using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RTCLib.Comm
{
    /// <summary>
    /// Send binary data through UDP
    /// </summary>
    public class UdpByteSender : IDisposable
    {
        // Local port to open Udp
        // int _localPort; // < disabled local wait

        // Remote host information
        private string _remoteHost = null;
        private int _remotePort;

        // udpClient client
        System.Net.Sockets.UdpClient _udpClient = null;

        // send target
        System.Net.IPEndPoint _remoteEndPoint = null;

        // event executed on data send completed
        /// <summary>
        /// Callback action on completing data sending
        /// </summary>
        public event Action SendCompleted;

        /// <summary>
        /// Constructor
        /// </summary>
        public UdpByteSender()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="remoteHost">Name or IP address of remote host to send</param>
        /// <param name="remotePort">Port number to send</param>
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

        /// <summary>
        /// Close socket if opened
        /// </summary>
        public void Close()
        {
            _remoteEndPoint = null;
            _udpClient ?.Close();
            _udpClient = null;
        }

        /// <summary>
        /// Send data packet synchronously 
        /// </summary>
        /// <param name="messageBytes"></param>
        /// <returns></returns>
        public int Send(byte[] messageBytes)
        {
            var ret = Send(messageBytes, _remoteEndPoint);
            return ret;
        }

        /// <summary>
        /// Send data packet synchronously
        /// </summary>
        /// <param name="messageBytes"></param>
        /// <param name="sendTo"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Send data packet asynchronously
        /// </summary>
        /// <param name="messageBytes">Data to send</param>
        /// <returns></returns>
        public async Task<int> SendAsync(byte[] messageBytes)
        {
            var ret = SendAsync(messageBytes, _remoteEndPoint);
            return await ret.ConfigureAwait(false);
        }

        /// <summary>
        /// Send data packet asynchronously
        /// </summary>
        /// <param name="messageBytes">Data to send</param>
        /// <param name="sendTo">Remote host to send</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<int> SendAsync(byte[] messageBytes, IPEndPoint sendTo)
        {
            Task<int> ret = null;
            if (messageBytes == null) throw new ArgumentNullException(nameof(messageBytes));
            ret = _udpClient.SendAsync(messageBytes, messageBytes.Length, sendTo);
            OnSendCompleted();
            return await ret.ConfigureAwait(false);
        }

        /// <summary>
        /// Event invoker
        /// </summary>
        protected virtual void OnSendCompleted()
        {
            SendCompleted?.Invoke();
        }


        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        /// <summary>
        /// Disposable pattern
        /// </summary>
        /// <param name="disposing">flag</param>
        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _udpClient?.Dispose();
            }
        }

        /// <summary>
        /// Disposable pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~UdpByteSender()
        {
            Dispose(false);
        }
    }
}