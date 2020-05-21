using System;
using System.Runtime.InteropServices;

namespace RTCLib.Comm
{
    public class UdpBinaryReceiver<T> : System.IDisposable
    {
        UdpByteReceiver _receiver;

        public UdpBinaryReceiver()
        {
            _receiver = null;
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
            return ret;
        }

        /// ターゲットになる構造体かクラスを
        /// そのままバイナリイメージで送信する
        public T Get()
        {
            byte[] data = _receiver.GetData(-1);
            if (data == null) return default(T);
            T ret = Sys.Interop.BytesToStructure<T>(data);

            return ret;

        }

        //-------------------- Dispose pattern

        ~UdpBinaryReceiver()
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
                _receiver?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}