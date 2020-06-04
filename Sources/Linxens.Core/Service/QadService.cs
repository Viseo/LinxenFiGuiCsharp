using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting;
using System.Threading;
using Linxens.Core.Logger;
using Linxens.Core.Model;
using Linxens.Core.QADServicesV2;
using Linxens.Core.QADServices;
using System.Linq;

namespace Linxens.Core.Service
{
    public class QadService
    {
        private readonly QadLogger _qadLogger;
        private readonly List<Tt_GolfDataType> QadDataRows;
        private readonly processGolfRepetitiveRequest QadRequest;
        
        private processGolfRepetitiveResponse QadResponse;
        //private readonly Temp_err_msg[] err = QadResponse.golfRepetitiveResponse.dsExceptions;
        public QadService(string ipAuthKey, string ipUser, string ipDomain)
        {
            TtContext context1 = new TtContext();
            context1.propertyQualifier = "QAD";
            context1.propertyName = "domain";
            context1.propertyValue = "4327";

            TtContext context2 = new TtContext();
            context2.propertyQualifier = "QAD";
            context2.propertyName = "version";
            context2.propertyValue = "CUST_1";

            TtContext context3 = new TtContext();
            context3.propertyQualifier = "QAD";
            context3.propertyName = "Operation";
            context3.propertyValue = "FI2QAD";

            TtContext[] contexts = new TtContext[] { context1, context2, context3 };

            this.QadRequest = new processGolfRepetitiveRequest();
            this.QadRequest.golfRepetitive = new WSDLGolfRepetitiveType();
            this.QadRequest.To = "urn:services-qad-com::QADERP";
            this.QadRequest.MessageID = "urn:services-qad-com::QADERP";
            this.QadRequest.ReferenceParameters = new ReferenceParametersType();
            this.QadRequest.ReferenceParameters.suppressResponseDetail = true;
            this.QadRequest.ReplyTo = new ReplyToType();
            this.QadRequest.ReplyTo.Address = "urn:services-qad-com:";

            this._qadLogger = QadLogger.Instance;

            this.QadDataRows = new List<Tt_GolfDataType>();
            //this.QadRequest = new xxf2q01Request(ipAuthKey, ipUser, ipDomain, this.QadDataRows.ToArray());
            this.QadRequest.golfRepetitive.dsSessionContext = contexts;

            //TODO: Si 
            this._qadLogger.LogInfo("QAD Service init", string.Format("QAD service init with : [User:{0}, Domain:{1}]", ipUser, ipDomain));

         
        }

        public bool Send(DataFile dataFile, out string error)
        {
            error = "";
            bool ret = false;
            string returnStatus = "";
            Stopwatch timer = new Stopwatch();
            this._qadLogger.LogInfo("Send data file to QAD service", "QAD service start...");
            try
            {
                QdocWebService qdocWebService = new QdocWebServiceClient("QdocWebService");

                // Register WR-SCRAP Datas
                int scrapNbr = 1;
                this._qadLogger.LogInfo("Prepare data for send", "Loading " + dataFile.Scrap.Count + " scrap(s)");
                timer.Start();
                foreach (Quality quality in dataFile.Scrap)
                {
                    Tt_GolfDataType currentScrap = new Tt_GolfDataType();

                    currentScrap.tt_srno = scrapNbr;
                    currentScrap.tt_emp = dataFile.Emp;
                    currentScrap.tt_tr_type = dataFile.TrType;
                    currentScrap.tt_line = dataFile.Line;
                    currentScrap.tt_part = dataFile.PN;
                    currentScrap.tt_op = dataFile.OP;
                    currentScrap.tt_wc = dataFile.WC;
                    currentScrap.tt_mch = dataFile.MCH;
                    currentScrap.tt_lbl = dataFile.LBL;
                    currentScrap.tt_t_lbl = dataFile.TapeN;
                    currentScrap.tt_qty = decimal.Parse(quality.Qty, CultureInfo.InvariantCulture);
                    currentScrap.tt_rsn = quality.RsnCode;
                    currentScrap.tt_defects = 0;
                    currentScrap.tt_splices = 0;

                    this.QadDataRows.Add(currentScrap);
                    scrapNbr++;
                }

                timer.Stop();
                this._qadLogger.LogInfo("Prepare data for send", "Loading scrap data DONE => Elapsed time : " + timer.Elapsed.Seconds + "sec");
                this._qadLogger.LogInfo("Prepare data for send", "Loading WR-BF-PROD data");
                timer.Reset();

                timer.Start();
                Tt_GolfDataType prodData = new Tt_GolfDataType();
                prodData.tt_srno = scrapNbr;
                prodData.tt_srnoSpecified = true;
                prodData.tt_emp = dataFile.Emp;
                prodData.tt_tr_type = "WR-BF-PROD";
                prodData.tt_line = dataFile.Line;
                prodData.tt_part = dataFile.PN;
                prodData.tt_op = dataFile.OP;
                prodData.tt_opSpecified = true;
                prodData.tt_wc = dataFile.WC;
                prodData.tt_mch = dataFile.MCH;
                prodData.tt_lbl = dataFile.LBL;
                prodData.tt_t_lbl = dataFile.TapeN;
                prodData.tt_qty = decimal.Parse(dataFile.Qty, CultureInfo.InvariantCulture);
                prodData.tt_qtySpecified = true;
                prodData.tt_rsn = "";
                prodData.tt_defects = dataFile.Defect;
                prodData.tt_defectsSpecified = true;
                prodData.tt_splices = dataFile.Splices;
                prodData.tt_splicesSpecified = true;
                prodData.tt_p_date = dataFile.DateTapes;
                prodData.tt_printer = dataFile.Printer;
                prodData.tt_shipto = "";
                this.QadDataRows.Add(prodData);

                timer.Stop();
                this._qadLogger.LogInfo("Prepare data for send", "Loading WR-BF-PROD DONE => Elapsed time : " + timer.Elapsed.Seconds + "sec");
                this._qadLogger.LogInfo("Prepare data for send", "Loading QAD request");
                timer.Reset();

                this._qadLogger.LogInfo("Send", "Sending data file start...");

                try
                {
                    this.QadRequest.golfRepetitive.dsGolfRepetitive = this.QadDataRows.ToArray();
                    timer.Start();
                    this.QadResponse = qdocWebService.golfRepetitive(this.QadRequest);
                    timer.Stop();
                    returnStatus = this.QadResponse.golfRepetitiveResponse.result;
                    if (returnStatus != "success") throw new ServerException();
                    this._qadLogger.LogInfo("Send", "SUCESS");
                    this._qadLogger.LogInfo("Send", "Elapsed time : " + timer.Elapsed.Seconds + "sec");
                    ret = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            catch (Exception e)
            {
                timer.Stop();
                if (string.IsNullOrWhiteSpace(returnStatus))
                {
                    this._qadLogger.LogError("Prepare data for send", e.Message);
                    this._qadLogger.LogError("Prepare data for send", "Elapsed time : " + timer.Elapsed.Seconds + "sec");
                    error = e.Message;
                }
                else
                {
                    this._qadLogger.LogError("Send data file to QAD service", e.Message);

                    if (this.QadResponse != null)
                    {
                        var tempErrorMsg = QadResponse.golfRepetitiveResponse.dsExceptions.FirstOrDefault();

                         if (tempErrorMsg != null)
                         {
                             string errorData = "[Desc: " + tempErrorMsg.tt_msg_desc + "]";
                             this._qadLogger.LogError("Send data file to QAD service", "\n" + errorData);
                             error += errorData + "\n";
                         }
                    }
                    this._qadLogger.LogError("Send data file to QAD service", "Elapsed time : " + timer.Elapsed.Seconds + "sec");
                }

                ret = false;
            }
            return ret;
        }
    }
}