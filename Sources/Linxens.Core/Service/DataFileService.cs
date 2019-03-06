using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Linxens.Core.Logger;
using Linxens.Core.Model;

namespace Linxens.Core.Service
{
    public class DataFileService
    {
        private readonly AppSettingsReader _config;
        private readonly ILogger _qadLogger;

        private readonly ILogger _technicalLogger;

        public DataFileService()
        {
            this._technicalLogger = TechnicalLogger.Instance;
            this._qadLogger = QadLogger.Instance;

            //_technicalLogger.LogError("test");

            this._config = new AppSettingsReader();
            this.CurrentFile = new DataFile {Scrap = new List<Quality>()};
            this.FilesToProcess = new List<string>();
            this.InitConfig();
            this.CheckDirectoryStructure(this.RootWorkingPath);
            this.LoadFileToProcess();
        }

        // Data for app.config
        public string UserApplication { get; set; }
        public string DomainWebService { get; set; }
        public string UserPwd { get; set; }
        public string MachineName { get; set; }
        public string UserEmail { get; set; }
        public string PrinterName { get; set; }
        public string WebServiceUrl { get; set; }
        public string WebServiceTimeOut { get; set; }

        public string RootDirPath { get; set; }

        // TODO => write in app.config on var update
        public string RootWorkingPath { get; set; }

        public DataFile CurrentFile { get; set; }

        public IList<string> FilesToProcess { get; set; }

        public void ReadFile(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException($"Path [{path}] not found");


            this.CurrentFile = new DataFile {Scrap = new List<Quality>()};

            string[] fileRawData = File.ReadAllLines(path);
            int currentLine = this.ReadFirstSection(fileRawData);
            currentLine = this.ReadScrapSection(fileRawData, currentLine);
            this.ReadLastSection(fileRawData, currentLine);
        }

        public string WriteFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            using (StreamWriter file =
                new StreamWriter(path))
            {
                foreach (var line in lines)
                {
                    if (line.StartsWith("Tape#"))
                    {
                        file.WriteLine(line);
                    }
                }

                return path;
            }
            //throw new NotImplementedException();
        }

        private void LoadFileToProcess()
        {
            string todoDir = Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString());

            List<string> realFiles = Directory.GetFiles(this.RootDirPath).ToList();
            if (realFiles.Any())
                foreach (string realFile in realFiles)
                {
                    string fileName = Path.GetFileName(realFile).Replace(".txt", $"_{DateTime.Now:yyyy-MM-dd  HH-mm-ss-fff}.txt");
                    if (fileName != null)
                    {
                        string destPath = Path.Combine(todoDir, fileName);
                        File.Copy(realFile, destPath, true);
                        File.Delete(realFile);
                    }
                }

            string[] todoFiles = Directory.GetFiles(Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString()));
            foreach (string todoFile in todoFiles) this.FilesToProcess.Add(todoFile);
        }

        private void InitConfig()
        {
            this.RootDirPath = this._config.GetValue("RootDirectory", typeof(string)) as string;
            this.RootWorkingPath = this._config.GetValue("RootWorkingDirectory", typeof(string)) as string;
        }

        /// <summary>
        ///     Read the first data section
        /// </summary>
        /// <param name="txtFile"></param>
        /// <returns></returns>
        private int ReadFirstSection(string[] txtFile)
        {
            int i;
            for (i = 0; i < txtFile.Length; i++)
            {
                string line = txtFile[i];
                if (line == "") return i;

                string[] items = line.Split(':');
                switch (items[0])
                {
                    case "Site":
                        this.CurrentFile.Site = items[1];
                        break;
                    case "Emp":
                        this.CurrentFile.Emp = items[1];
                        break;
                    case "Tr-Type":
                        this.CurrentFile.TrType = items[1];
                        break;
                    case "Line":
                        this.CurrentFile.Line = items[1];
                        break;
                    case "PN":
                        this.CurrentFile.PN = items[1];
                        break;
                    case "OP":
                        this.CurrentFile.OP = Convert.ToInt32(items[1]);
                        break;
                    case "WC":
                        this.CurrentFile.WC = items[1];
                        break;
                    case "MCH":
                        this.CurrentFile.MCH = items[1];
                        break;
                    case "Lbl":
                        this.CurrentFile.LBL = items[1];
                        break;
                }
            }

            return i;
        }

        /// <summary>
        ///     read data for scrap section
        /// </summary>
        /// <param name="txtFile"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private int ReadScrapSection(string[] txtFile, int startIndex)
        {
            string tape = "";
            int i;
            for (i = startIndex + 1; i < txtFile.Length; i++)
            {
                string line = txtFile[i];
                if (txtFile[i] == "") break;

                string[] items = line.Split(':');
                switch (items[0])
                {
                    case "Tape#":
                        tape = items[1];
                        break;
                    case "Qty":
                        this.CurrentFile.Scrap.Add(new Quality
                        {
                            Tape = tape,
                            Qty = items[1].Split(' ')[0],
                            RsnCode = items[2]
                        });
                        break;
                }
            }

            return i;
        }

        /// <summary>
        ///     read the last part of the file
        /// </summary>
        /// <param name="txtFile"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private int ReadLastSection(string[] txtFile, int startIndex)
        {
            int i;
            for (i = startIndex + 1; i < txtFile.Length; i++)
            {
                string line = txtFile[i];
                if (txtFile[i] == "") break;
                string[] items = line.Split(':');
                switch (items[0])
                {
                    case "Qty":
                        this.CurrentFile.Qty = items[1];
                        break;
                    case "Defect":
                        this.CurrentFile.Defect = Convert.ToInt32(items[1]);
                        break;
                    case "Splices":
                        this.CurrentFile.Splices = Convert.ToInt32(items[1]);
                        break;
                    case "Dates":
                        this.CurrentFile.DateTapes = items[1];
                        break;
                    case "Printer":
                        this.CurrentFile.Printer = items[1];
                        break;
                    case "Number of conform parts":
                        this.CurrentFile.NumbOfConfParts = items[1];
                        break;
                }
            }

            return i;
        }

        /// <summary>
        ///     Check if directory structure exist
        /// </summary>
        /// <param name="path"></param>
        private void CheckDirectoryStructure(string path)
        {
            string todoDir = Path.Combine(path, WorkingType.TODO.ToString());
            string runningDir = Path.Combine(path, WorkingType.RUNNING.ToString());
            string doneDir = Path.Combine(path, WorkingType.DONE.ToString());
            string errorsDir = Path.Combine(path, WorkingType.ERROR.ToString());

            bool todoExist = Directory.Exists(todoDir);
            bool runningExist = Directory.Exists(runningDir);
            bool doneExist = Directory.Exists(doneDir);
            bool errorsExist = Directory.Exists(errorsDir);

            if (!todoExist) Directory.CreateDirectory(todoDir);

            if (!runningExist) Directory.CreateDirectory(runningDir);

            if (!doneExist) Directory.CreateDirectory(doneDir);

            if (!errorsExist) Directory.CreateDirectory(errorsDir);
        }

        private enum WorkingType
        {
            TODO,
            RUNNING,
            DONE,
            ERROR
        }
    }
}