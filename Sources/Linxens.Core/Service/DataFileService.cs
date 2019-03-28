using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using Linxens.Core.Logger;
using Linxens.Core.Model;

namespace Linxens.Core.Service
{
    public class DataFileService
    {
        private readonly AppSettingsReader _config;

        public readonly ILogger _technicalLogger;

        public DataFileService()
        {
            this._technicalLogger = TechnicalLogger.Instance;

            this._config = new AppSettingsReader();
            this.FilesToProcess = new List<string>();

            try
            {
                this.InitConfig();
            }
            catch (Exception)
            {
                this._technicalLogger.LogError("Init Configuration", "Error on directory init. Please check your configuration file");
            }

            try
            {
                this.CheckDirectoryStructure(this.RootWorkingPath);
                this.LoadFileToProcess();
            }
            catch (Exception)
            {
                this._technicalLogger.LogError("Init Configuration", "Error on working directory structure init. Please check your configuration file");
            }
        }

        public string RootDirPath { get; set; }

        // TODO => write in app.config on var update
        public string RootWorkingPath { get; set; }

        public DataFile CurrentFile { get; set; }

        public IList<string> FilesToProcess { get; set; }

        public DataFile ReadFile(string fileName)
        {
            string todoDir = Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString());
            string fullPath = Path.Combine(todoDir, fileName);
            // Possible si le fichier exist déja dans TODO. Dans le cas d'un import de fichier "on ne peut pas faire un File.Exists dans TODO 
            if (!File.Exists(fullPath))
            {
                this._technicalLogger.LogError("Read File", string.Format("Failed to read file. The file on path [{0}] is not a valid FI Station", fullPath));
                return null;
            }


            DataFile dataFile = new DataFile {Scrap = new List<Quality>()};

            try
            {
                string[] fileRawData = File.ReadAllLines(fullPath);

                int currentLine = this.ReadFirstSection(ref dataFile, fileRawData);
                currentLine = this.ReadScrapSection(ref dataFile, fileRawData, currentLine);
                this.ReadLastSection(ref dataFile, fileRawData, currentLine);
            }
            catch (Exception)
            {
                this._technicalLogger.LogError("Read File", string.Format("[{0}] is not a FI Station File", fileName));
                return null;
            }

            this._technicalLogger.LogInfo("Read File", string.Format("[{0}] loaded successfully", fileName));
            dataFile.FilePath = fullPath;

            return dataFile;
        }

        public bool VerifFile(string fileName)
        {
            DataFile tmpDataFile = this.ReadFile(fileName);

            if (tmpDataFile == null)
            {
                this._technicalLogger.LogError("Load File", string.Format("The File [{0}] is not a valid FI Station", fileName));
                return false;
            }

            this._technicalLogger.LogInfo("Load File", string.Format("The File [{0}] is loaded successfully", fileName));
            return true;
        }

        public void WriteFile()
        {
            List<string> tab = new List<string>();

            tab.Add("Repetitive:");
            tab.Add("Site:" + this.CurrentFile.Site);
            tab.Add("Emp:" + this.CurrentFile.Emp);
            tab.Add("Tr-Type:" + this.CurrentFile.TrType);
            tab.Add("Line:" + this.CurrentFile.Line);
            tab.Add("PN:" + this.CurrentFile.PN);
            tab.Add("OP:" + this.CurrentFile.OP);
            tab.Add("WC:" + this.CurrentFile.WC);
            tab.Add("MCH:" + this.CurrentFile.MCH);
            tab.Add("Lbl:" + this.CurrentFile.LBL);
            tab.Add("");
            tab.Add("Tape#:" + this.CurrentFile.TapeN);
            foreach (Quality quality in this.CurrentFile.Scrap) tab.Add("Qty:" + quality.Qty.ToString(CultureInfo.InvariantCulture) + " " + "Rsn Code:" + quality.RsnCode);
            tab.Add("");
            tab.Add("WR-PROD:");
            tab.Add("Tape#:" + this.CurrentFile.TapeN);
            tab.Add("Qty:" + this.CurrentFile.Qty);
            tab.Add("Defect:" + this.CurrentFile.Defect);
            tab.Add("Splices:" + this.CurrentFile.Splices);
            tab.Add("Dates:" + this.CurrentFile.DateTapes);
            tab.Add("Printer:" + this.CurrentFile.Printer);
            tab.Add("Number of conform parts:" + this.CurrentFile.NumbOfConfParts);

            File.AppendAllLines(Path.Combine(this.RootWorkingPath, WorkingType.RUNNING.ToString(), "runningFile.txt"), tab);
        }

        public void successFile()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            File.Move(Path.Combine(this.RootWorkingPath, WorkingType.RUNNING.ToString(), "runningFile.txt"), Path.Combine(this.RootWorkingPath, WorkingType.DONE.ToString(), "RunningReelSuccess_" + date + ".txt"));
            File.Delete(Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString(), this.CurrentFile.FilePath));
            this._technicalLogger.LogInfo("Send data", this.CurrentFile.FilePath + "moved in DONE directory");
        }

        public void ErrorFile()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            File.Move(Path.Combine(this.RootWorkingPath, WorkingType.RUNNING.ToString(), "runningFile.txt"), Path.Combine(this.RootWorkingPath, WorkingType.ERROR.ToString(), "RunningReelERROR_" + date + ".txt"));
            this._technicalLogger.LogError("Send data", "The data file"+ this.CurrentFile.FilePath +" was moved in ERROR directory");
        }

        public void LoadFileToProcess()
        {
            string todoDir = Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString());

            List<string> realFiles = Directory.GetFiles(this.RootDirPath).ToList();
            if (realFiles.Any())
                foreach (string realFile in realFiles)
                {
                    if(!realFile.Contains(".txt"))
                    {
                        this._technicalLogger.LogWarning("Load files", string.Format("The file {0} is not a file text", realFile));
                        continue;
                    }

                    string fileName = Path.GetFileName(realFile).Replace(".txt", string.Format("_{0:yyyy-MM-dd_HH-mm-ss-fff}.txt", DateTime.Now));
                    this._technicalLogger.LogInfo("Creation file to process", string.Format("The file [{0}] is created successfully", fileName));
                    if (fileName != null)
                    {
                        string destPath = Path.Combine(todoDir, fileName);
                        File.Copy(realFile, destPath, true);
                        this._technicalLogger.LogInfo("Copy file on TODO directory", string.Format("The file [{0}] is copied on the TODO directory successfully", fileName));
                        File.Delete(realFile);
                        this._technicalLogger.LogInfo("Delete File on the working directory", string.Format("The file [{0}] is deleted successfully on the working directory", fileName));
                    }
                }

            this.FilesToProcess = new DirectoryInfo(Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString())).GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Where(f => f.Name.Contains(".txt"))
                .Select(f => f.Name)
                .ToArray();
        }

        private void InitConfig()
        {
            this.RootDirPath = this._config.GetValue("RootDirectory", typeof(string)) as string;
            this._technicalLogger.LogInfo("Init root directory", "The root directory is initialized successfully");
            this.RootWorkingPath = this._config.GetValue("RootWorkingDirectory", typeof(string)) as string;
            this._technicalLogger.LogInfo("Init root working directory", "The root working directory is initialized successfully");
        }

        /// <summary>
        ///     Read the first data section
        /// </summary>
        /// <param name="txtFile"></param>
        /// <returns></returns>
        private int ReadFirstSection(ref DataFile datafile, string[] txtFile)
        {
            int i;
            for (i = 0; i < txtFile.Length; i++)
            {
                string line = txtFile[i];
                if (line == "") return i;

                string[] items = line.Split(':');
                switch (items[0])
                {
                    case "Repetitive":
                        break;
                    case "Site":
                        datafile.Site = items[1];
                        break;
                    case "Emp":
                        datafile.Emp = items[1];
                        break;
                    case "Tr-Type":
                        datafile.TrType = items[1];
                        break;
                    case "Line":
                        datafile.Line = items[1];
                        break;
                    case "PN":
                        datafile.PN = items[1];
                        break;
                    case "OP":
                        datafile.OP = Convert.ToInt32(items[1]);
                        break;
                    case "WC":
                        datafile.WC = items[1];
                        break;
                    case "MCH":
                        datafile.MCH = items[1];
                        break;
                    case "Lbl":
                        datafile.LBL = items[1];
                        break;
                    default:
                        this._technicalLogger.LogError("Read file", "Unrecognized value \"" + items[0] + "\" in the first section of the file");
                        throw new ArgumentException();
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
        private int ReadScrapSection(ref DataFile datafile, string[] txtFile, int startIndex)
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
                        datafile.Scrap.Add(new Quality
                        {
                            Qty = items[1].Split(' ')[0],
                            RsnCode = items[2]
                        });
                        break;
                    default:
                        this._technicalLogger.LogError("Read file", "Unrecognized value \"" + items[0] + "\" in the scrap list section of the file");
                        throw new ArgumentException();
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
        private int ReadLastSection(ref DataFile datafile, string[] txtFile, int startIndex)
        {
            string qty = "";
            int i;
            for (i = startIndex + 1; i < txtFile.Length; i++)
            {
                string line = txtFile[i];
                if (txtFile[i] == "") break;
                string[] items = line.Split(':');
                switch (items[0])
                {
                    case "WR-PROD":
                        break;

                    case "Qty":
                        datafile.Qty = items[1];
                        qty = items[1];
                        break;
                    case "Defect":
                        datafile.Defect = Convert.ToInt32(items[1]);
                        break;
                    case "Splices":
                        datafile.Splices = Convert.ToInt32(items[1]);
                        break;
                    case "Dates":
                        datafile.DateTapes = items[1];
                        break;
                    case "Printer":
                        datafile.Printer = items[1];
                        break;
                    case "Number of conform parts":
                        datafile.NumbOfConfParts = items[1];
                        break;
                    case "Tape#":
                        datafile.TapeN = items[1];
                        break;
                    default:
                        this._technicalLogger.LogError("Read file", "Unrecognized value \"" + items[0] + "\" in the last section of the file");
                        throw new ArgumentException();
                }
            }

            decimal totalScrap = 0;
            foreach (Quality quality in datafile.Scrap)
            {
                bool isValid = true;
                decimal current;
                isValid = decimal.TryParse(quality.Qty, NumberStyles.Float, CultureInfo.InvariantCulture, out current);

                if (isValid) totalScrap += current;
            }

            decimal currentQty = decimal.Parse(qty, CultureInfo.InvariantCulture);
            decimal initialQty = currentQty + totalScrap;
            datafile.InitialQty = initialQty.ToString(CultureInfo.InvariantCulture);

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
            this._technicalLogger.LogInfo("Creation directory", "TODO directory is created successfully");
            if (!runningExist) Directory.CreateDirectory(runningDir);
            this._technicalLogger.LogInfo("Creation directory", "RUNNING directory is created successfully");
            if (!doneExist) Directory.CreateDirectory(doneDir);
            this._technicalLogger.LogInfo("Creation directory", "DONE directory is created successfully");
            if (!errorsExist) Directory.CreateDirectory(errorsDir);
            this._technicalLogger.LogInfo("Creation directory", "ERRORS directory is created successfully");
        }

        public string MoveToTODODirectory(string filePath)
        {
            string fileName = Path.GetFileName(filePath).Replace(".txt", string.Format("_{0:yyyy-MM-dd_HH-mm-ss-fff}.txt", DateTime.Now));
            File.Copy(filePath, Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString(), fileName));
            return fileName;
        }

        public void DeleteFromTodoDirectory(string fileName)
        {
            File.Delete(Path.Combine(this.RootWorkingPath, WorkingType.TODO.ToString(), fileName));
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