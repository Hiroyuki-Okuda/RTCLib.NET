using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using System.Runtime.InteropServices;

namespace RTCLib
{
    public class UdpByteReceiver
    {
        // Local port to receive
        private int localPort;
        
        // Target remote host
        // Packet from other remote hosts will be discarded 
        // if the target remote host is directed.
        // Please use IPAddress.Any if you allow any remote host.
        private IPAddress remoteHost;
        private IPEndPoint remotEndPoint;

        private System.Net.Sockets.UdpClient udpClient;

        private System.Net.IPEndPoint received_ep;

        // received packets
        private LinkedList<byte[]> receivedPackets;

        // mutex for async receive
        private System.Threading.Mutex mutex = new System.Threading.Mutex();

        /// <summary>
        /// Open udpClient and listen for the port from 'any host'
        /// </summary>
        /// <param name="_localPort">listening port</param>
        /// <returns></returns>
        public int Open( int _localPort ) {
            localPort = _localPort;

            // UDPクライアントを作成
            udpClient = new System.Net.Sockets.UdpClient(localPort);
            
            // 送信先エンドポイント情報を作成
            received_ep = new IPEndPoint(IPAddress.Any, 0);

            // 非同期受信を開始
            udpClient.BeginReceive(ReceiveCallBack, this);

            return 0;
        }

        /// <summary>
        /// Open udpClient and listen for the port from directed host
        /// </summary>
        /// <param name="_localPort">listening port</param>
        /// <returns></returns>
        public int Open(int _localPort, IPAddress _remoteHost)
        {
            localPort = _localPort;

            // Create UDP client
            remoteHost = _remoteHost;
            remotEndPoint = new IPEndPoint(_remoteHost, _localPort);
            udpClient = new System.Net.Sockets.UdpClient(remotEndPoint);

            // 非同期受信を開始
            udpClient.BeginReceive(ReceiveCallBack, this);

            return 0;
        }


        public void ClearBuffer() 
        {
            mutex.WaitOne();
            receivedPackets.Clear();
            mutex.ReleaseMutex();
        }

        public byte[] GetAllData()
        {
            mutex.WaitOne();
            int size = 0;
            //for( int i = 0; i < receivedPackets.Count; i++)
            //{
            //    size += receivedPackets[i].Length;
            //}
            size = receivedPackets.Sum(o => o.Length);

            byte[] ret = new byte[size];
            int position = 0;
            //for( int i = 0; i < receivedPackets.Count; i++)
            //{
            //    receivedPackets[i].CopyTo( ret, index );
            //    index += receivedPackets[i].Length;
            //}
            foreach (var receivedPacket in receivedPackets)
            {
                receivedPacket.CopyTo(ret, position);
                position += receivedPacket.Length;
            }

            mutex.ReleaseMutex();
            return ret;
        }

        /// <summary>
        /// n番目のデータを返す
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public byte[] GetData(int n = -1)
        {
            if (receivedPackets.Count == 0 || receivedPackets.Count <= n) return null;
            if (n == -1) n = receivedPackets.Count - 1;
            
            mutex.WaitOne();
            //int last_index = receivedPackets.Count-1;
            byte[] ret = new byte[receivedPackets.ElementAt(n).Length];
            receivedPackets.ElementAt(n).CopyTo(ret, 0);
            mutex.ReleaseMutex();
            return ret;
        }
        
        private void ReceiveCallBack(IAsyncResult Asr)
        {
            byte[] dat = udpClient.EndReceive( Asr, ref received_ep);

            mutex.WaitOne();
            receivedPackets.AddLast(dat);
            mutex.ReleaseMutex();

            // 今一度非同期受信を開始
            udpClient.BeginReceive(ReceiveCallBack, this);
        }

        public UdpByteReceiver(int n = 100)
        {
            receivedPackets = new LinkedList<byte[]>();
        }

        ~UdpByteReceiver()
        {
            received_ep = null;
            udpClient.Close();
            receivedPackets.Clear();
            receivedPackets = null;
            udpClient = null;
        }

    }


    public class UdpSender<T>
    {
        UdpByteSender sender;

        public UdpSender()
        {
            sender = null;
        }

        ~UdpSender()
        {
            sender = null;
        }

        /// <summary>
        /// UDPで送信するポートを開く
        /// </summary>
        /// <param name="_remote_host">送信先ホスト</param>
        /// <param name="_remote_port">送信先ポート</param>
        /// <param name="_local_port">送信元ポート</param>
        /// <returns></returns>
        public int Open( string _remote_host, int _remote_port, int _local_port)
        {
            sender = new UdpByteSender();
            int ret = sender.Open(_remote_host, _remote_port);
            return ret;
        }

        /// ターゲットになる構造体かクラスを
        /// そのままバイナリイメージで送信する
        public int Send(T trg)
        {
            int size = Marshal.SizeOf(trg);
            byte[] bytes = new byte[size];
            GCHandle gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(trg, gch.AddrOfPinnedObject(), false);

            sender.Send(bytes);
            gch.Free();

            return 0;
        }

    }

    public class UdpReceiver<T>
    {
        UdpByteReceiver recv;

        public UdpReceiver()
        {
            recv = null;
        }

        ~UdpReceiver()
        {
            recv = null;
        }

        /// <summary>
        /// UDPの受信ポートを指定して開く
        /// </summary>
        /// <param name="_local_port">UDP受信を受け付けるポート番号</param>
        /// <returns></returns>
        public int Open( int _local_port)
        {
            recv = new UdpByteReceiver();
            int ret = recv.Open(_local_port);
            return ret;
        }

        /// ターゲットになる構造体かクラスを
        /// そのままバイナリイメージで送信する
        public T Get()
        {
            int size = Marshal.SizeOf( typeof(T) );
            //byte[] bytes = new byte[size];

            byte[] data = recv.GetData(-1);
            if (data == null) return default(T);
            //data.CopyTo(bytes, 0);

            GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);

            T ret = (T)(Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T)));

            gch.Free();
            return ret;
        }

    }

    /// <summary>
    /// UDP Parameter sender
    /// </summary>
    /// Send the parameter by UDP,
    /// No dependability, but simple.
    /// Sending data is:
    /// All data should be ascii, printable code.
    /// Each command is separated by "\n".
    /// Each command contains "Key" and "Data".
    /// "Key" and "Data" are separated by ":"
    /// Key is parameter name, data is value.
    /// This class doesnt support type conversion, 
    /// only sending string is supported.
    public class UdpParameterSender
    { 
        int lcl_port;       /// local port

        string remote_host; /// remote host to deliver parameter
        int rmt_port;       /// remote port to deliver

        System.Net.Sockets.UdpClient udp;   /// udp client

        System.Net.IPEndPoint remoteEP;     /// endpoint
               
        /// <summary>
        /// Open UDP
        /// </summary>
        /// <param name="remote_host">Remote IP</param>
        /// <param name="remote_port">Remote Port</param>
        /// <param name="local_port">Sending local port</param>
        /// <returns></returns>
        public int Open( string _remote_host, int _remote_port, int _local_port ) {
            remote_host = _remote_host;
            lcl_port = _local_port;
            rmt_port = _remote_port;   

            // Create UDP client
            udp = new System.Net.Sockets.UdpClient(lcl_port);

            // Enable Broadcast
            udp.EnableBroadcast = true;

            // Set remote endpoint
            remoteEP = new IPEndPoint(IPAddress.Parse(remote_host), rmt_port);

            udp.Connect(remoteEP);

            return 0;
        }

        public int Send(string key, string data)
        {
            string msg = key + ":" + data + "\n";
            byte[] buf = Encoding.ASCII.GetBytes(msg);
            return udp.Send(buf, buf.Length);
        }

        ~UdpParameterSender()
        {
            remoteEP = null;
            udp.Close();
            udp = null;
        }
    }

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
