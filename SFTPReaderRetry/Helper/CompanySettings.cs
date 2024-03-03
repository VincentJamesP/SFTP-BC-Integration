using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFTPReaderRetry.Helper
{
    public class CompanySettings
    {
        public static string SFTPServer => Environment.GetEnvironmentVariable("SFTPServer");
        public static int SFTPPort => int.Parse(Environment.GetEnvironmentVariable("SFTPPort"));
        public static string SFTPUsername => Environment.GetEnvironmentVariable("SFTPUsername");
        public static string SFTPPassword => Environment.GetEnvironmentVariable("SFTPPassword");
        public static string FolderPath => Environment.GetEnvironmentVariable("FolderPath");
        public static string[] Branches => Environment.GetEnvironmentVariable("Branches")?.Split(',');
    }
}
