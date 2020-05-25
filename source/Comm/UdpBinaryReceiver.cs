using System;
using System.Runtime.InteropServices;

namespace RTCLib.Comm
{
    /// <summary>
    /// Receive binary struct through UDP
    /// </summary>
    /// <typeparam name="T">Struct type to receive</typeparam>
    public class UdpBinaryReceiver<T> : System.IDisposable
    where T : struct
    {
        UdpByteReceiver _receiver;

        private T _lastReceived;

        /// <summary>
        /// Data received last time
        /// </summary>
        public T LastReceived
        {
            get => _lastReceived;
            private set => _lastReceived = value;
        }

        /// <summary>
        /// Validity of LastReceived data; 
        /// </summary>
        public bool IsDataAvailable { get; } = false;

        /// <summary>
        /// Handler on the data receive
        /// </summary>
        /// <param name="receivedData">Received data structure</param>
        public delegate void ReceivedBytesHandler(T receivedData);

        /// <summary>
        /// Event on receiving binary data
        /// </summary>
        public event ReceivedBytesHandler OnDataReceived;


        /// <summary>
        /// Constructor
        /// </summary>
        public UdpBinaryReceiver()
        {
            _receiver = null;
            IsDataAvailable = false;
            LastReceived = default(T);
        }

        /// <summary>
        /// UDPの受信ポートを指定して開く
        /// </summary>
        /// <param name="localPort">UDP受信を受け付けるポート番号</param>
        /// <returns></returns>
        public int Open( int localPort)
        {
            _receiver = new UdpByteReceiver();
            int ret = _receiver.Open(localPort);
            _receiver.OnBytesReceived += OnByteDataReceived;
            return ret;
        }

        /// <summary>
        /// Close socket
        /// </summary>
        public void Close()
        {
            _receiver?.Close();
            _receiver = null;
        }

        /// ターゲットになる構造体かクラスを
        /// そのままバイナリイメージで送信する
        public T Get()
        {
            byte[] data = _receiver.GetData(-1);
            if (data == null) return default(T);

            T ret = Sys.Interop.BytesToStructure<T>(data);
            LastReceived = ret;
            return ret;
        }

        private void OnByteDataReceived(byte[] data)
        {
            if (data == null) return;
            if(data.Length != Marshal.SizeOf<T>())return;
            LastReceived = Sys.Interop.BytesToStructure<T>(data);
            OnDataReceived?.Invoke(_lastReceived);
        }

        //-------------------- Dispose pattern

        /// <inheritdoc />
        ~UdpBinaryReceiver() => Dispose(false);

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
                _receiver?.Dispose();
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
    }
}