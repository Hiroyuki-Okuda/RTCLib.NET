using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace RTCLib.Comm
{
    /// <summary>
    /// Binary data receiver through Udp
    /// </summary>
    public class UdpByteReceiver : IDisposable
    {
        // Local port to receive
        private int _localPort;

        // Target remote host
        // Packet from other remote hosts will be discarded 
        // if the target remote host is directed.
        // Please use IPAddress.Any if you allow any remote host.
        private IPEndPoint _remotEndPoint = null;

        private System.Net.Sockets.UdpClient _udpClient;

        private System.Net.IPEndPoint _receivedEp;

        // received packets
        private readonly LinkedList<byte[]> _receivedPackets;

        // mutex for async receive
        private readonly System.Threading.Mutex _mutex = new System.Threading.Mutex();

        /// <summary>
        /// Handler on the data gram receive
        /// </summary>
        /// <param name="dataBytes"></param>
        public delegate void ReceivedBytesHandler(byte[] dataBytes);

        //! Event on receiving binary data
        event ReceivedBytesHandler OnBytesReceived;

        /// <summary>
        /// Constructor
        /// </summary>
        public UdpByteReceiver()
        {
            _receivedPackets = new LinkedList<byte[]>();
        }

        /// <summary>
        /// Open udpClient and listen for the port from 'any host'
        /// </summary>
        /// <param name="localPort">listening port</param>
        /// <returns></returns>
        public int Open(int localPort)
        {
            _localPort = localPort;

            // UDPクライアントを作成
            _udpClient = new System.Net.Sockets.UdpClient(_localPort);

            // 送信先エンドポイント情報を作成
            _receivedEp = new IPEndPoint(IPAddress.Any, 0);

            // 非同期受信を開始
            _udpClient.BeginReceive(ReceiveCallBack, this);

            return 0;
        }

        /// <summary>
        /// Open udpClient and listen for the port from directed host
        /// </summary>
        /// <param name="remoteHost">Remote host to listen. Use IPAddress.Any if you listen to any host</param>
        /// <param name="localPort">listening port</param>
        /// <returns></returns>
        public int Open(IPAddress remoteHost, int localPort)
        {
            this._localPort = localPort;

            // Create UDP client
            _remotEndPoint = new IPEndPoint(remoteHost, localPort);
            _udpClient = new System.Net.Sockets.UdpClient(_remotEndPoint);

            // 非同期受信を開始
            _udpClient.BeginReceive(ReceiveCallBack, this);

            return 0;
        }

        /// <summary>
        /// Open udpClient and listen for the port from directed host
        /// </summary>
        /// <param name="remoteHost">Remote host to listen. Use IPAddress.Any if you listen to any host</param>
        /// <param name="localPort">listening port</param>
        /// <returns></returns>
        public int Open(string remoteHost, int localPort)
        {
            this._localPort = localPort;

            // Create UDP client
            _remotEndPoint = new IPEndPoint( IPAddress.Parse(remoteHost), localPort);
            _udpClient = new System.Net.Sockets.UdpClient(_remotEndPoint);

            // 非同期受信を開始
            _udpClient.BeginReceive(ReceiveCallBack, this);

            return 0;
        }

        /// <summary>
        /// Closing socket
        /// </summary>
        public void Close()
        {
            _udpClient?.Close();
            _udpClient = null;
            ClearBuffer();
        }


        /// <summary>
        /// Clear buffer
        /// </summary>
        public void ClearBuffer() 
        {
            _mutex.WaitOne();
            _receivedPackets.Clear();
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Get all received data
        /// </summary>
        /// <returns></returns>
        public byte[] GetAllData()
        {
            _mutex.WaitOne();

            int size = 0;
            //for( int i = 0; i < receivedPackets.Count; i++)
            //{
            //    size += receivedPackets[i].Length;
            //}
            size = _receivedPackets.Sum(o => o.Length);

            byte[] ret = new byte[size];
            int position = 0;
            //for( int i = 0; i < receivedPackets.Count; i++)
            //{
            //    receivedPackets[i].CopyTo( ret, index );
            //    index += receivedPackets[i].Length;
            //}
            foreach (var receivedPacket in _receivedPackets)
            {
                receivedPacket.CopyTo(ret, position);
                position += receivedPacket.Length;
            }

            _mutex.ReleaseMutex();
            return ret;
        }

        /// <summary>
        /// Get available data packets count
        /// </summary>
        /// <returns></returns>
        public int GetAvailableDataCount()
        {
            return _receivedPackets.Count;
        }

        /// <summary>
        /// n番目のデータを返す
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public byte[] GetData(int n = -1)
        {
            if (_receivedPackets.Count == 0 || _receivedPackets.Count <= n) return null;
            if (n == -1)
            {
                _mutex.WaitOne();
                byte[] ret1 = _receivedPackets.Last();
                _receivedPackets.RemoveLast();
                _mutex.ReleaseMutex();
                return ret1;
            }
            
            _mutex.WaitOne();
            //int last_index = receivedPackets.Count-1;
            byte[] ret = new byte[_receivedPackets.ElementAt(n).Length];
            _receivedPackets.ElementAt(n).CopyTo(ret, 0);
            _mutex.ReleaseMutex();
            return ret;
        }
        
        private void ReceiveCallBack(IAsyncResult Asr)
        {
            byte[] dat = _udpClient.EndReceive( Asr, ref _receivedEp);

            OnBytesReceived?.Invoke(dat);

            if (OnBytesReceived == null)
            {
                // store the data if callback is not set
                _mutex.WaitOne();
                _receivedPackets.AddLast(dat);
                _mutex.ReleaseMutex();
            }


            // 今一度非同期受信を開始
            _udpClient.BeginReceive(ReceiveCallBack, this);
        }

 
        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        /// <summary>
        /// Disposable pattern
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _udpClient?.Dispose();
                _mutex?.Dispose();
                _receivedPackets?.Clear();
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
        ~UdpByteReceiver()
        {
            Dispose(false);
        }
    }
}