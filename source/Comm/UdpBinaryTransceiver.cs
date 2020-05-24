using System;
using System.Net;
using System.Runtime.InteropServices;

namespace RTCLib.Comm
{
    /// <summary>
    /// Receive binary struct through UDP
    /// </summary>
    /// <typeparam name="T">Struct type to receive</typeparam>
    public class UdpBinaryTransceiver<T> : System.IDisposable
    where T : struct
    {
        private UdpByteReceiver _receiver;
        private UdpByteSender _sender;

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


        // /// <summary>
        // /// Constructor
        // /// </summary>
        //public UdpBinaryTranceiver()
        //{
        //    _receiver = null;
        //    _sender = null;
        //    IsDataAvailable = false;
        //    LastReceived = default(T);
        //}

        /// <summary>
        /// Open Udp client 
        /// </summary>
        /// <param name="hostToSend">remote host to send including the port.
        /// use broadcast for broadcasting to all hosts</param>
        /// <param name="localPort">UDP port number to listen</param>
        /// <param name="hostToListen">Remote host to listen. Use IPAddress.Any or use default if you listen any host.</param>
        /// <returns></returns>
        public UdpBinaryTransceiver( IPEndPoint hostToSend, int localPort, IPAddress hostToListen = null)
        {
            if(hostToListen == null) hostToListen = IPAddress.Any;
            _receiver = new UdpByteReceiver(hostToListen, localPort);
            _receiver.OnBytesReceived += OnByteDataReceived;
            _sender = new UdpByteSender(hostToSend);
        }

        /// <summary>
        /// Open Udp client 
        /// </summary>
        /// <param name="remoteIp">Remote IP to send</param>
        /// <param name="remotePort">Remote port to send</param>
        /// <param name="localPort">Receive port to listen</param>
        /// <param name="hostToListen">Remote host to listen. Use IPAddress.Any or use default if you listen any host.</param>
        /// <returns></returns>
        public UdpBinaryTransceiver(string remoteIp, int remotePort, int localPort, IPAddress hostToListen = null)
        : this(new IPEndPoint(IPAddress.Parse(remoteIp), remotePort), localPort, hostToListen )
        {
        }

        /// <summary>
        /// Close socket and initialize
        /// </summary>
        public void Close()
        {
            _sender?.Close();
            _receiver?.Close();
            _sender = null;
            _receiver = null;
        }

        /// <summary>
        /// Send structure data by binary send
        /// </summary>
        /// <param name="target">data to send</param>
        /// <returns></returns>
        public int Send(T target)
        {
            byte[] data = RTCLib.Sys.Interop.StructureToBytes(target);
            _sender.Send(data);
            return 0;
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
        ~UdpBinaryTransceiver() => Dispose(false);

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
                _sender?.Dispose();
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