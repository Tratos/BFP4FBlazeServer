using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace BFP4FBlazeServer
{
    public enum LogLevel
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        Data = 16,
        All = 31
    }

    public static class Logger
    {
        private static LogLevel _logLevel;
        private static StringBuilder _writeString;
        public static readonly object _sync = new object();

        public static string logFile;

        public static void Clear()
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }

        public static void Initialize(string filename, LogLevel logLevel, bool clear)
        {
            logFile = filename;
            _logLevel = logLevel;
            _writeString = new StringBuilder();

            if(clear)
            {
                Clear();
            }
        }

            private static void Write(string message, LogLevel level)
            {
            lock (_sync)
            {
                StackTrace trace = new StackTrace();
                StackFrame frame = null;

                frame = trace.GetFrame(2);

                ConsoleColor color = ConsoleColor.White;
                switch (level)
                {
                    case LogLevel.Debug:
                        message = "DEBUG: " + message;
                        color = ConsoleColor.DarkGray;
                        break;
                    case LogLevel.Info:
                        message = " INFO: " + message;
                        break;
                    case LogLevel.Warning:
                        message = " WARN: " + message;
                        color = ConsoleColor.DarkYellow;
                        break;
                    case LogLevel.Error:
                        message = "ERROR: " + message;
                        color = ConsoleColor.DarkRed;
                        break;
                    case LogLevel.Data:
                        message = " DATA: " + message;
                        color = ConsoleColor.DarkGreen;
                        break;
                }

                string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " - " + message;

                Console.WriteLine(text, Console.ForegroundColor = color);
                Console.ForegroundColor = ConsoleColor.White;

                if (!MayWriteType(level))
                {
                    return;
                }

                _writeString.AppendLine(text);


                WriteAway();
            }
        }

        private static bool MayWriteType(LogLevel type)
        {
            return ((_logLevel & type) == type);
        }

        public static void WriteAway()
        {
            string stringToWrite = _writeString.ToString();
            _writeString.Length = 0;

            Thread.BeginCriticalRegion();
            StreamWriter _logWriter;
            _logWriter = new StreamWriter(logFile, true);
            _logWriter.Write(stringToWrite);
            _logWriter.Flush();
            _logWriter.Close();
            _logWriter.Dispose();
            Thread.EndCriticalRegion();
        }

        public static void Data(string message)
        {
            Write(message, LogLevel.Data);
        }

        public static void Error(string message)
        {
            Write(message, LogLevel.Error);
        }

        public static void Warn(string message)
        {
            Write(message, LogLevel.Warning);
        }

        public static void Info(string message)
        {
            Write(message, LogLevel.Info);
        }

        public static void Debug(string message)
        {
            if (!Config.useDebug)
            {
                return;
            }

            Write(message, LogLevel.Debug);
        }   
    }
}
