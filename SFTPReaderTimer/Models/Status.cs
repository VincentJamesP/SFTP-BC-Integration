using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFTPReaderTimer.Models
{
    public class Status
    {
        public string branch_code { get; set; }
        public string company_code { get; set; }
        public string detail_count { get; set; }
        public string payment_count { get; set; }
        public string record { get; set; }
        public string sysdate { get; set; }
        public string timestamp { get; set; }
        public string version { get; set; }
    }
}
