using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace Linxens.Core.Logger
{
    public sealed class TechnicalLogger : ILogger
    {
        private const string source = "FI Auto Data Entry";

        private static readonly Lazy<TechnicalLogger> lazy =
            new Lazy<TechnicalLogger>(() => new TechnicalLogger());

        private TechnicalLogger()
        {
            AppSettingsReader appSettingsReader = new AppSettingsReader();
            this._logFilePath = appSettingsReader.GetValue("LogDirectory", typeof(string)) as string;

            try
            {
                if (string.IsNullOrWhiteSpace(this._logFilePath)) throw new ArgumentException();

                Directory.CreateDirectory(this._logFilePath);
                this.LogInfo("","Directory created");
            }
            catch (Exception)
            {
                // TODO: crate the log directory in %APPDATA%
                string dirPath = "Logs";
                Directory.CreateDirectory(dirPath);
                this._logFilePath = dirPath;
                // TODO vrai message
                this.LogError("", "Log directory not specified");
            }
        }

        private string _logFilePath { get; set;}
        private string _logFileName {get {return "Linxens_test.log";}}

        public static TechnicalLogger Instance { get { return lazy.Value; } }

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

            using (EventLog eventLog = new EventLog("Application"))
            {
                try
                {
                    File.AppendAllLines(Path.Combine(this._logFilePath, this._logFileName), new[] { lineLog });

                    eventLog.Source = source;
                    //eventLog.WriteEntry($"{action}: {message}", (EventLogEntryType) level, 1);
                }
                catch (Exception e)
                {
                    // TODO
                }
            }

        }
    }
}