using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecutor
{
    public interface ILogger
    {
        void Log(string message);

        void Log(string message, params object[] values);

        void Flush();
    }

    public class Logger : ILogger
    {
        private const string logFileNameFormat = "dd.MM.yyyy HH.MM.ss";

        /// <summary>
        /// Contains the log file path.
        /// </summary>
        private string _logFile;

        private StringBuilder _logBuilder;

        public Logger(string logFileFolderPath)
        {
            _logBuilder = new StringBuilder();

            // Create the full file path for the new log
            var logFileName = DateTime.Now.ToString(logFileNameFormat) + ".txt";
            var logFileFullPath = Path.Combine(logFileFolderPath, logFileName);

            // Create the log file it self
            File.Create(logFileFullPath);

            _logFile = logFileFullPath;
        }

        public void Log(string message)
        {
            Log(message, string.Empty);
        }

        public void Log(string message, params object[] values)
        {
            var messageToWrite = string.Format(message, values);
            _logBuilder.Append(string.Format("{0}: {1}{2}", DateTime.UtcNow, messageToWrite, Environment.NewLine));
        }

        public void Flush()
        {
            File.AppendAllText(_logFile, _logBuilder.ToString(), Encoding.UTF8);
            _logBuilder.Clear();
        }
    }
}
