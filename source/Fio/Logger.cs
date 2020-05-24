using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

namespace RTCLib.Fio
{
    public class Logger
    {
        
        internal class LogItem{
            public string message;
            public int level;
            public LogItem(int lvl, string msg) 
            {
                level = lvl;
                message = msg;
            }
        }

        public delegate void WriteLogDelegate(dynamic dev, string message);
        public delegate void TerminationDelegate(dynamic dev);

        public class LogDevice
        {
            public WriteLogDelegate action;
            public TerminationDelegate term;
            public dynamic device;
            public int level;

            public LogDevice(int _level, dynamic dev, WriteLogDelegate act,
                TerminationDelegate trm = null)
            {
                level = _level;
                device = dev;
                action = act;
                term = trm;

            }
        }

        static List<LogItem> logs;
        static List<LogDevice> devices;
        static global::System.Threading.Mutex log_mutex = new global::System.Threading.Mutex();
        static global::System.Threading.Mutex device_mutex = new global::System.Threading.Mutex();

        static bool isStopping;
        static global::System.Timers.Timer tim;

        ~Logger()
        {
            Term();
        }

        
        static public void Init()
        {
            init_device();
            init_logs();
            isStopping = false;

            tim = new global::System.Timers.Timer(10);
            
            tim.Elapsed += tim_Elapsed;
            tim.Start();
        }
        static public void Term()
        {
            isStopping = true;
 
            while (logs.Count > 0)
            {
                global::System.Threading.Thread.Sleep(1);
            }
            logs.Clear();
            tim.Dispose();
            tim = null;
            
            lock (device_mutex)
            {
                foreach (var x in devices)
                {
                    if (x.term != null) 
                    {
                        x.term(x.device);
                    }
                }

                devices.Clear();
            }
            //logs = null;
            //devices = null;

        }

        static void tim_Elapsed(object sender, global::System.Timers.ElapsedEventArgs e)
        {
            if (logs.Count > 0)
            {
                lock (log_mutex) {
                    if (logs.Count > 0)
                    {
                        LogItem tmp = logs[0];
                        logs.RemoveAt(0);
                        lock(device_mutex)
                        {
                            foreach (var x in devices)
                            {
                                if (x.level <= tmp.level)
                                {
                                    x.action(x.device, tmp.message);
                                }
                            }
                        }
                    }
                }
            }
        }

        static private void init_device()
        {
            if (devices == null)
            {

                lock (device_mutex)
                {
                    if (devices == null)
                        devices = new List<LogDevice>(5);
                }
            }
        }
        static private void init_logs()
        {
            if(logs==null)
            {
                lock (log_mutex)
                {
                    if (logs == null)
                        logs = new List<LogItem>(10000);
                }
            }
        }


        static public void register_log_device(int level, dynamic dev, 
            WriteLogDelegate s, TerminationDelegate term=null)
        {
            init_device();
            lock (device_mutex)
            {
                devices.Add(new LogDevice(level, dev, s, term));
            }
        }

        /// <summary>
        /// Add the log message
        /// </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="level">Logging levels of message</param>
        /// level 0 : show anyway
        /// level n : show message if n if g.e. than levels of device
        static public void AddLog(string msg, int level=0) 
        {
            if (isStopping) return;
            lock (log_mutex)
            {
                logs.Add(new LogItem(level, msg) );
            }
        }

        static public void Notion(string msg, int level = 0)
        { 
            if (isStopping) return;
            lock (log_mutex)
            {
                logs.Add(new LogItem(level, msg) );
            }
            global::System.Windows.Forms.MessageBox.Show(msg);
        }

    }
}
