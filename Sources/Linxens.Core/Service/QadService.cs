using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting;
using System.ServiceModel;
using Linxens.Core.Logger;
using Linxens.Core.Model;
using Linxens.Core.QADServices;

namespace Linxens.Core.Service
{
    public class QadService
    {
        private readonly QadLogger _qadLogger;
        private readonly string ipAuthKey;
        private readonly string ipDomain;
        private readonly string ipUser;

        private readonly List<xxf2q01_tt_golf_dataRow> QadDataRows;
        private xxf2q01_tt_Error_WarningRow[] QadErrorRows;
        private readonly xxf2q01Request QadRequest;

        public QadService(string ipAuthKey, string ipUser, string ipDomain)
        {
            this._qadLogger = QadLogger.Instance;

            this.ipAuthKey = ipAuthKey;
            this.ipUser = ipUser;
            this.ipDomain = ipDomain;

            this.QadDataRows = new List<xxf2q01_tt_golf_dataRow>();
            this.QadRequest = new xxf2q01Request(this.ipAuthKey, this.ipUser, this.ipDomain, this.QadDataRows.ToArray());
        }

        public bool Send(DataFile dataFile)
        {
            string returnStatus = "";
            Stopwatch timer = new Stopwatch();
            this._qadLogger.LogInfo("Send data file to QAD service", "QAD service start...");
            try
            {
                using (MES2QAD_ASObjClient qadClient = new MES2QAD_ASObjClient("MES2QAD_ASObj", "http://test-qad01.mic.ad:8111/wsa/wsa1"))
                {
                    string tape = dataFile.Scrap.First().Tape;

                    this.QadErrorRows = new xxf2q01_tt_Error_WarningRow[dataFile.Scrap.Count + 1];

                    // Register WR-SCRAP Datas
                    int scrapNbr = 1;
                    this._qadLogger.LogInfo("Prepare data for send", "Loading " + dataFile.Scrap.Count + " scrap(s)");
                    timer.Start();
                    foreach (Quality quality in dataFile.Scrap)
                    {
                        xxf2q01_tt_golf_dataRow currentScrap = new xxf2q01_tt_golf_dataRow();

                        currentScrap.ip_srno = scrapNbr;
                        currentScrap.ip_emp = dataFile.Emp;
                        currentScrap.ip_tr_type = dataFile.TrType;
                        currentScrap.ip_line = dataFile.Line;
                        currentScrap.ip_part = dataFile.PN;
                        currentScrap.ip_op = dataFile.OP;
                        currentScrap.ip_wc = dataFile.WC;
                        currentScrap.ip_mch = dataFile.MCH;
                        currentScrap.ip_lbl = dataFile.LBL;
                        currentScrap.ip_t_lbl = tape;
                        currentScrap.ip_qty = decimal.Parse(quality.Qty, CultureInfo.InvariantCulture);
                        currentScrap.ip_rsn = quality.RsnCode;
                        currentScrap.ip_defects = 0;
                        currentScrap.ip_splices = 0;
                        currentScrap.ip_p_date = null;
                        currentScrap.ip_printer = null;
                        currentScrap.ip_shipto = null;

                        this.QadDataRows.Add(currentScrap);
                        scrapNbr++;
                    }

                    timer.Stop();
                    this._qadLogger.LogInfo("Prepare data for send", "Loading scrap data DONE => Elapsed time : " + timer.Elapsed.Seconds + "sec");
                    this._qadLogger.LogInfo("Prepare data for send", "Loading WR-BF-PROD data");
                    timer.Reset();

                    timer.Start();
                    xxf2q01_tt_golf_dataRow prodData = new xxf2q01_tt_golf_dataRow();
                    prodData.ip_srno = scrapNbr;
                    prodData.ip_emp = dataFile.Emp;
                    prodData.ip_tr_type = "WR-BF-PROD";
                    prodData.ip_line = dataFile.Line;
                    prodData.ip_part = dataFile.PN;
                    prodData.ip_op = dataFile.OP;
                    prodData.ip_wc = dataFile.WC;
                    prodData.ip_mch = dataFile.MCH;
                    prodData.ip_lbl = dataFile.LBL;
                    prodData.ip_t_lbl = tape;
                    prodData.ip_qty = decimal.Parse(dataFile.Qty, CultureInfo.InvariantCulture);
                    prodData.ip_rsn = "";
                    prodData.ip_defects = dataFile.Defect;
                    prodData.ip_splices = dataFile.Splices;
                    prodData.ip_p_date = dataFile.DateTapes;
                    prodData.ip_printer = dataFile.Printer;
                    prodData.ip_shipto = "Dummy";

                    timer.Stop();
                    this._qadLogger.LogInfo("Prepare data for send", "Loading WR-BF-PROD DONE => Elapsed time : " + timer.Elapsed.Seconds + "sec");
                    this._qadLogger.LogInfo("Prepare data for send", "Loading QAD request");
                    timer.Reset();

                    this._qadLogger.LogInfo("Send", "Sending data file start...");

                    timer.Start();
                    qadClient.xxf2q01(this.QadRequest.ip_AuthKey, this.QadRequest.ip_User, this.QadRequest.ip_domain, this.QadDataRows.ToArray(), out returnStatus, out this.QadErrorRows);
                    timer.Stop();
                    if (returnStatus != "ok") throw new ServerException();
                }
            }
            catch (Exception e)
            {
                if (string.IsNullOrWhiteSpace(returnStatus))
                {
                    timer.Stop();
                    this._qadLogger.LogError("Prepare data for send", e.Message);
                    this._qadLogger.LogError("Prepare data for send", "Elapsed time : " + timer.Elapsed.Seconds + "sec");
                }
                else
                {
                    this._qadLogger.LogError("Send data file to QAD service", e.Message);
                    foreach (xxf2q01_tt_Error_WarningRow xxf2Q01TtErrorWarningRow in this.QadErrorRows)
                        this._qadLogger.LogError("Send data file to QAD service", "" +
                                                                                  "[Status: " + xxf2Q01TtErrorWarningRow.tterr_code + "]\n" +
                                                                                  "[Type: " + xxf2Q01TtErrorWarningRow.tterr_type + "]\n" +
                                                                                  "[Code: " + xxf2Q01TtErrorWarningRow.tterr_code + "]\n" +
                                                                                  "[Desc: " + xxf2Q01TtErrorWarningRow.tterr_desc + "]");

                    this._qadLogger.LogError("Send data file to QAD service", "Elapsed time : " + timer.Elapsed.Seconds + "sec");
                }

                return false;
            }

            return true;
        }
    }
}