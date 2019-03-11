using System;
using System.Collections.Generic;
using Linxens.Core.Model;
using Linxens.Core.QADServices;

namespace Linxens.Core.Service
{
    public class QadService
    {
        private string ipAuthKey;
        private string ipDomain;
        private string ipUser;
        private List<xxf2q01_tt_golf_dataRow> QadDataRows;
        private List<xxf2q01_tt_Error_WarningRow> QadErrorRows;
        private xxf2q01Request QadRequest;
        private xxf2q01Response QadResponse;

        public QadService(string ipAuthKey, string ipUser, string ipDomain)
        {
            this.ipAuthKey = ipAuthKey;
            this.ipUser = ipUser;
            this.ipDomain = ipDomain;

            this.QadDataRows = new List<xxf2q01_tt_golf_dataRow>();
            this.QadErrorRows = new List<xxf2q01_tt_Error_WarningRow>();
        }

        public string Send(DataFile dataFile, out string returnStatus)
        {
            throw new NotImplementedException();
        }
    }
}