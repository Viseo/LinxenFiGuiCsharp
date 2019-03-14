using Linxens.Core.Service;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Linxens.Core.Model
{
    public class DataFile
    {
        public string Site { get; set; }
        public string Emp { get; set; }
        public string TrType { get; set; }
        public string Line { get; set; }
        public string PN { get; set; }
        public int? OP { get; set; }
        public string WC { get; set; }
        public string MCH { get; set; }
        public string LBL { get; set; }


        public List<Quality> Scrap { get; set; }


        public string InitialQty { get; set; }

        private string _qty;
        public string Qty
        {
            get
            {
                float totalScrap = 0f;
                foreach (Quality quality in this.Scrap)
                {
                    bool isValid = true;
                    float current;
                    isValid = float.TryParse(quality.Qty, NumberStyles.Float, CultureInfo.InvariantCulture, out current);

                    if (isValid)
                        totalScrap += current;
                }

                //foreach (Quality quality in this.Scrap) totalScrap += float.Parse(quality.Qty, CultureInfo.InvariantCulture);
                float initialFloat = float.Parse(this.InitialQty, CultureInfo.InvariantCulture);
                return (initialFloat + totalScrap).ToString(CultureInfo.InvariantCulture);
            }
            set { this._qty = value; }
        }

        public int? Defect { get; set; }
        public int? Splices { get; set; }
        public string DateTapes { get; set; }
        public string Printer { get; set; }
        public string NumbOfConfParts { get; set; }

        public string TapeN { get; set; }
    }
    
    public class Quality
    {
        public string Qty { get; set; }
        public string RsnCode { get; set; }   
    }
}