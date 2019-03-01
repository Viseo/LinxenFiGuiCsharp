using System.Collections.Generic;

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


        //public string Tape { get; set; }
        public string Qty { get; set; }
        public int? Defect { get; set; }
        public int? Splices { get; set; }
        public string DateTapes { get; set; }
        public string Printer { get; set; }
        public string NumbOfConfParts { get; set; }
    }

    public class Quality
    {
        public string Tape { get; set; }
        public string Qty { get; set; }
        public string RsnCode { get; set; }
    }
}