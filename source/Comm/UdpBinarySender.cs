using System;
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
            Open(remoteHost, remotePort);
        }

        /// <summary>
        /// UDPで送信するポートを開く
        /// </summary>
        /// <param name="remoteHost">送信先ホスト</param>
        /// <param name="remotePort">送信先ポート</param>
        /// <returns></returns>
        public int Open( string remoteHost, int remotePort)
        {
            _sender = new UdpByteSender();
            int ret = _sender.Open(remoteHost, remotePort);
            return ret;
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