using System;
using System.Configuration;
using System.IO;
using System.Windows.Forms.VisualStyles;
using Linxens.Core.Helper;

namespace Linxens.Core.Logger
{
    public sealed class TechnicalLogger : ILogger
    {
        public static Action<string> logUi;

        private const string source = "FI Auto Data Entry";

        private static readonly Lazy<TechnicalLogger> lazy =
            new Lazy<TechnicalLogger>(() => new TechnicalLogger());

        private TechnicalLogger()
        {
            AppSettingsReader appSettingsReader = new AppSettingsReader();
            this._logFilePath = appSettingsReader.GetValue("LogDirectory", typeof(string)) as string;

            // Configuration is empty
            if (string.IsNullOrWhiteSpace(this._logFilePath))
            {
                this.CreateDefaultLogDir();

                this.LogWarning("Log Init", "No log directory given in the configuration file");
                this.LogInfo("Log Init", "Log directory was created on " + this._logFilePath);
                return;
            }

            bool dirLogExist = Directory.Exists(this._logFilePath);

            if (dirLogExist)
            {
                bool writeAccess = Helper.Helper.HasWritePermissionOnDir(this._logFilePath);
                if (writeAccess)
                {
                    this.LogInfo("Log Init", "Log directory was created on " + this._logFilePath);
                }
                else
                {
                    this.CreateDefaultLogDir();

                    this.LogWarning("Log Init", "Log directory in configuration file has no write access");
                    this.LogInfo("Log Init", "Log directory was created on " + this._logFilePath);
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(this._logFilePath);
                    bool writeAccess = Helper.Helper.HasWritePermissionOnDir(this._logFilePath);

                    if (writeAccess)
                    {
                        this.LogInfo("Log Init", "Log directory was created on " + this._logFilePath);
                    }
                    else
                    {
                        this.CreateDefaultLogDir();

                        this.LogWarning("Log Init", "Log directory in configuration file has no write access");
                        this.LogInfo("Log Init", "Log directory was created on " + this._logFilePath);
                    }
                }
                catch (Exception)
                {
                    string givenDir = this._logFilePath;
                    this.CreateDefaultLogDir();
                    this.LogWarning("Log Init", "An error has throw on creating log directory given in configuration file : " + givenDir);
                    this.LogInfo("Log Init", "Log directory was created on " + this._logFilePath);
                }
            }
        }

        private string _logFilePath { get; set; }

        private string _logFileName
        {
            get
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                return "TechnicalLog_" + date + ".log";
            }
        }

        public static TechnicalLogger Instance
        {
            get { return lazy.Value; }
        }

        public void LogInfo(string action, string message)
        {
            this.Log(LoggerEnum.LogLevel.INFO, action, message);
        }

        public void LogWarning(string action, string message)
        {
            this.Log(LoggerEnum.LogLevel.WARN, action, message);
        }

        public void LogError(string action, string message)
        {
            this.Log(LoggerEnum.LogLevel.ERROR, action, message);
        }

        private void CreateDefaultLogDir()
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), source);
            if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);

            this._logFilePath = appDataFolder;
        }

        private void Log(LoggerEnum.LogLevel level, string action, string message)
        {
            try
            {
                string lineLog = DateTime.Now.ToString("s") + "|" + level.ToString(5) + "|" + action + " >> " + message;
                File.AppendAllLines(Path.Combine(this._logFilePath, this._logFileName), new[] {lineLog});
                logUi(lineLog);
            }
            catch (Exception)
            {
                Console.Error.Write("");
            }
        }
    }
}