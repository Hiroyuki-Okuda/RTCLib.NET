using System;
using System.Net;
using System.Runtime.InteropServices;

namespace RTCLib.Comm
{
    /// <summary>
    /// Send binary struct through Udp
    /// </summary>
    /// <typeparam name="T">Type to send</typeparam>
    public class UdpBinarySender<T> : System.IDisposable
    {
        UdpByteSender _sender = null;
        
        /// <summary>
        /// Construct without open
        /// </summary>
        public UdpBinarySender()
        {
        }

        /// <summary>
        /// Construct with open 
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        public UdpBinarySender(string remoteHost, int remotePort)
        {
            _sender = new UdpByteSender(remoteHost, remotePort);
        }


        /// <summary>
        /// Construct with open 
        /// </summary>
        /// <param name="remoteEndPoint">Remote endpoint(IPAddress+Port) to send</param>
        public UdpBinarySender(IPEndPoint remoteEndPoint)
        {
            _sender = new UdpByteSender(remoteEndPoint);
        }


        /// <summary>
        /// Closing socket
        /// </summary>
        public void Close()
        {
            _sender?.Close();
            _sender = null;
        }


        /// ターゲットになる構造体かクラスを
        /// そのままバイナリイメージで送信する
        public int Send(T trg)
        {
            byte[] data = RTCLib.Sys.Interop.StructureToBytes(trg);
            _sender.Send(data);

            return 0;
        }

        //-------------------- Dispose pattern

        /// <inheritdoc />
        ~UdpBinarySender() => Dispose(false);

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