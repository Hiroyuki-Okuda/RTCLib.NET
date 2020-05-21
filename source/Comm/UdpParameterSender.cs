using System.Net;
using System.Text;

namespace RTCLib.Comm
{
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
        int rmt_port;       //! remote port to deliver

        System.Net.Sockets.UdpClient udp;   //! udp client

        System.Net.IPEndPoint remoteEP;     //! endpoint
               
        /// <summary>
        /// Open Udp socket
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public int Open( string remoteHost, int remotePort, int localPort ) {
            remote_host = remoteHost;
            lcl_port = localPort;
            rmt_port = remotePort;   

            // Create UDP client
            udp = new System.Net.Sockets.UdpClient(lcl_port);

            // Enable Broadcast
            udp.EnableBroadcast = true;

            // Set remote endpoint
            remoteEP = new IPEndPoint(IPAddress.Parse(remote_host), rmt_port);

            udp.Connect(remoteEP);

            return 0;
        }

        /// <summary>
        /// Send pair of key and data
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Send(string key, string data)
        {
            string msg = key + ":" + data + "\n";
            byte[] buf = Encoding.ASCII.GetBytes(msg);
            return udp.Send(buf, buf.Length);
        }

        /// <inheritdoc />
        ~UdpParameterSender()
        {
            remoteEP = null;
            udp.Close();
            udp = null;
        }
    }
}