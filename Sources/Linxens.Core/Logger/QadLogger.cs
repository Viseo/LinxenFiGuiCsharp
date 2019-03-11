using System;
using System.Configuration;
using System.IO;

namespace Linxens.Core.Logger
{
    public sealed class QadLogger : ILogger
    {
        private static readonly Lazy<QadLogger> lazy =
            new Lazy<QadLogger>(() => new QadLogger());

        private QadLogger()
        {
            AppSettingsReader appSettingsReader = new AppSettingsReader();
            this.LogFilePath = appSettingsReader.GetValue("LogDirectory", typeof(string)) as string;

            try
            {
                if (string.IsNullOrWhiteSpace(this._logFilePath)) throw new ArgumentException();

                Directory.CreateDirectory(this._logFilePath);
                this.LogInfo("", "This directory exist");
            }
            catch (Exception)
            {
                string dirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogsQAD");
                Directory.CreateDirectory(dirPath);
                this._logFilePath = dirPath;
                this.LogError("", "Log directory not specified");
            }
        }

        private string _logFilePath { get; set;}
        private string _logFileName {get {return "test.log";}}

        private string LogFilePath { get; set;}

        public static QadLogger Instance { get { return lazy.Value; } }

        public void LogInfo(string action, string message)
        {
            this.Log(LoggerEnum.LogLevel.INFO, action, message);
        }

        public void LogWarning(string action, string message)
        {
            this.Log(LoggerEnum.LogLevel.WARNING, action, message);
        }

        public void LogError(string action, string message)
        {
            this.Log(LoggerEnum.LogLevel.ERROR, action, message);
        }

        private void Log(LoggerEnum.LogLevel level, string action, string message)
        {
            string lineLog = DateTime.Now + "\t" + "|" + level + "\t" + "|" + action + "\t" + "|" + message;
            File.AppendAllLines(Path.Combine(this._logFilePath, this._logFileName), new[] {lineLog});
        }
    }
}