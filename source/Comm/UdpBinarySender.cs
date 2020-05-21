using System;
using System.Runtime.InteropServices;

namespace RTCLib.Comm
{
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

        ~UdpBinarySender()
        {
            Dispose(false);
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _sender?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}