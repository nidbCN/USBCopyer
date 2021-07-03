using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USBCopyer.Utils
{
    public static class MyLogger
    {
        public static EventLog logger = new EventLog()
        {
            Source = Application.ProductName,
        };

        public enum LogType
        {
            Info, Warning, Error
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="str">日志内容</param>
        /// <param name="type">日志类型（0=INFO,1=WARN,2=ERROR）</param>
        public static void Log(string message, LogType type = LogType.Info)
        {
            EventLogEntryType eventType;
            switch (type)
            {
                case LogType.Info:
                    eventType = EventLogEntryType.Information;
                    break;
                case LogType.Warning:
                    eventType = EventLogEntryType.Warning;
                    break;
                case LogType.Error:
                    eventType = EventLogEntryType.Error;
                    break;
                default:
                    eventType = EventLogEntryType.Information;
                    break;
            }

            Console.Write(message);
            logger.WriteEntry(message, eventType);
        }
    }
}
