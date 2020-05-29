using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace RTCLib.Sys
{
    /// <summary>
    /// Logging class 
    /// </summary>
    /// 
    /// Event logging class.
    /// 
    /// Before starting a log,
    /// the device should be registered with notification level.
    /// All message has also a notification level.
    /// 
    /// The message which has a higher notification level 
    /// than the devices' one sent to the device.
    /// 
    public class MessageLogger : IDisposable
    {

        /// <summary>
        /// Logging level
        /// </summary>
        /// If logging level of message is equal or large than
        /// device logging level, the message is sent to the device.
        /// 
        /// Lowest(100) &lt; Low(200) &lt; Middle(300) &lt; High(400) &lt; Highest(500)
        /// or use int value.
        public enum LoggingLevel : int
        {
            /// <summary>Lowest log level for not important message</summary>
            Lowest = 100,
            /// <summary>Lower log level for less important message</summary>
            Low = 200,
            /// <summary>Middle log level; base line</summary>
            Middle = 300,
            /// <summary>High log level for more important message</summary>
            High = 400,
            /// <summary>Highest log level for the most important message</summary>
            Highest = 500,
        }

        private class LogItem{
            public readonly string Message;
            public readonly LoggingLevel Level;
            public LogItem(LoggingLevel level, string message) 
            {
                Level = level;
                Message = message;
            }

            public override string ToString() => Level + ":" + Message;
        }

        /// <summary>
        /// Delegate to write the log message to device.
        /// </summary>
        /// <param name="device">Device object as dynamic</param>
        /// <param name="message">Message to write</param>
        /// Registered devices are called as:
        /// WriteLogDelegate(device, message).
        public delegate void WriteLogDelegate(dynamic device, string message);

        /// <summary>
        /// Delegate for device termination
        /// </summary>
        /// <param name="device">Device object as dynamic</param>
        /// This delegate is called when the logger is closed.
        /// If the device is inheriting IDisposable, Dispose()
        /// is also called when MessageLogger is disposed.
        public delegate void TerminationDelegate(dynamic device);

        /// <summary>
        /// 
        /// </summary>
        private class LogDevice
        {
            public readonly WriteLogDelegate Action;
            public readonly TerminationDelegate Termination;
            public readonly dynamic Device;
            public readonly LoggingLevel Level;

            public LogDevice(LoggingLevel level, dynamic device, WriteLogDelegate action,
                TerminationDelegate termination = null)
            {
                Level = level;
                Device = device;
                Action = action;
                Termination = termination;
            }
        }

        private readonly List<LogItem> _logs = new List<LogItem>();
        private readonly List<LogDevice> _devices = new List<LogDevice>();
        private readonly System.Threading.Mutex _logMutex = new System.Threading.Mutex();
        private readonly System.Threading.Mutex _deviceMutex = new System.Threading.Mutex();

        private System.Timers.Timer _timer = null;

        /// <summary>
        /// Is going to close logger
        /// </summary>
        private bool _isStopping;


        /// <summary>
        /// Initialization 
        /// </summary>
        /// <param name="interval">Interval to call </param>
        /// This method set the timer to call the procedure
        /// to output the log to device.
        /// If interval is negative, user have to call
        /// ProcessMessage() manually to output the log.
        public void StartLog(int interval)
        {
            _isStopping = false;

            if (interval > 0)
            {
                _timer = new global::System.Timers.Timer(interval);
                _timer.Elapsed += TimerCallback;
                _timer.Start();
            }
        }
        /// <summary>
        /// Terminate logger system
        /// </summary>
        public void Terminate()
        {
            _isStopping = true;

            WaitForLogComplete();
            _timer?.Dispose();
            _timer = null;


            lock (_deviceMutex)
            {
                foreach (var x in _devices)
                {
                    // if termination func is registered, do termination
                    if (x.Termination != null) 
                    {
                        x.Termination(x.Device);
                    }
                    // if the object inheriting IDisposable, dispose it
                    IDisposable dx = x as IDisposable;
                    dx?.Dispose();
                }

                _devices.Clear();
            }
        }

        /// <summary>
        /// Wait until all messages are processed.
        /// </summary>
        public void WaitForLogComplete()
        {
            if (_logs != null)
            {
                // wait until all messages are processed
                int n = _logs.Count;
                while (n > 0)
                {
                    // ProcessMessage(); // process message by timer, to process in consistent thread
                    global::System.Threading.Thread.Sleep(1);
                    lock (_logs)
                    {
                        n = _logs.Count;
                    }
                }
            }
        }

        /// <summary>
        /// Executed in other thread
        /// </summary>
        private void TimerCallback(object sender, global::System.Timers.ElapsedEventArgs e)
        {
            ProcessMessage();
        }

        /// <summary>
        /// Process message
        /// </summary>
        /// Output queued messages to the device
        public void ProcessMessage()
        {
            Debug.Assert(_logs != null, nameof(_logs) + " != null");
            if (_logs.Count > 0)
            {
                lock (_logMutex) {
                    if (_logs.Count > 0)
                    {
                        LogItem logItem = _logs[0];
                        lock(_deviceMutex)
                        {
                            foreach (var x in _devices)
                            {
                                if (x.Level <= logItem.Level)
                                {
                                    x.Action(x.Device, logItem.Message);
                                }
                            }
                        }
                        _logs.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Add logging device class
        /// </summary>
        /// <param name="level">Logging level</param>
        /// <param name="dev">Device class as dynamic</param>
        /// <param name="writeDelegate">Delegate to write message to the device</param>
        /// <param name="termDelegate">Delegate to terminate the device</param>
        ///
        /// Set lower log level if you want to log less important messages.
        /// Set higher log level for digest to log only important messages.
        ///
        /// Terminanation delegate is called when the logger is terminated.
        public void AddLogDevice(LoggingLevel level, dynamic dev, 
            WriteLogDelegate writeDelegate, TerminationDelegate termDelegate=null)
        {
            Debug.Assert(_deviceMutex != null, nameof(_deviceMutex) + " != null");
            lock (_deviceMutex)
            {
                _devices.Add(new LogDevice(level, dev, writeDelegate, termDelegate));
            }
        }

        /// <summary>
        /// Add the log message
        /// </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="level">Logging levels of message</param>
        /// level 0 : show anyway
        /// level n : show message if n if g.e. than levels of device
        public void AddLog(string msg, LoggingLevel level=0) 
        {
            if (_isStopping) return;
            lock (_logMutex)
            {
                _logs.Add(new LogItem(level, msg) );
            }
        }

        // ---------------------- disposable pattern

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        /// <inheritdoc />
        ~MessageLogger() => Dispose(false);

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                Terminate();
                _logMutex?.Dispose();
                _deviceMutex?.Dispose();
                _timer?.Dispose();
            }
        }

        /// <summary>
        /// Dispose unmanaged/managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
