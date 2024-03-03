using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFTPReaderNotification.Helper
{
    public class CompanySettings
    {
        public static string SFTPServer => Environment.GetEnvironmentVariable("SFTPServer");
        public static int SFTPPort => int.Parse(Environment.GetEnvironmentVariable("SFTPPort"));
        public static string SFTPUsername => Environment.GetEnvironmentVariable("SFTPUsername");
        public static string SFTPPassword => Environment.GetEnvironmentVariable("SFTPPassword");
        public static string SMTPServer => Environment.GetEnvironmentVariable("SMTPServer");
        public static int SMTPPort => int.Parse(Environment.GetEnvironmentVariable("SMTPPort"));
        public static string SenderEmail => Environment.GetEnvironmentVariable("SenderEmail");
        public static string SenderPassword => Environment.GetEnvironmentVariable("SenderPassword");
        public static string RecipientEmail => Environment.GetEnvironmentVariable("RecipientEmail");
        public static string EmailSubject => Environment.GetEnvironmentVariable("EmailSubject");
        public static string EmailBody => Environment.GetEnvironmentVariable("EmailBody");
        public static string FolderPath => Environment.GetEnvironmentVariable("FolderPath");
        public static string[] Branches => Environment.GetEnvironmentVariable("Branches")?.Split(',');
    }
}
