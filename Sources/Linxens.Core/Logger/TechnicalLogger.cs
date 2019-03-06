using System;
using System.Configuration;
using System.IO;

namespace Linxens.Core.Logger
{
    public sealed class TechnicalLogger : ILogger
    {
        private static readonly Lazy<TechnicalLogger> lazy =
            new Lazy<TechnicalLogger>(() => new TechnicalLogger());

        private TechnicalLogger()
        {
            AppSettingsReader appSettingsReader = new AppSettingsReader();
            this._logFilePath = appSettingsReader.GetValue("LogDirectory", typeof(string)) as string;
            try
            {
                if (string.IsNullOrWhiteSpace(this._logFilePath)) throw new ArgumentException();
                this.LogError("Directory creation", "Directory is created on the default path. You have select any value or whitespace");

                Directory.CreateDirectory(this._logFilePath);
                this.LogInfo("Directory creation","Directory creates successfully on the path"  + this._logFilePath );
            }
            catch (Exception)
            {
                string dirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(dirPath);
                this._logFilePath = dirPath;
                // TODO vrai message
                this.LogError("Directory creation for all files and located on TODO directory", "Log directory not specified");
            }
        }

        private string _logFilePath { get; }
        private string _logFileName => $"Linxens_{DateTime.Now.ToString("yyyy-MM-dd")}.log";

        public static TechnicalLogger Instance => lazy.Value;

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