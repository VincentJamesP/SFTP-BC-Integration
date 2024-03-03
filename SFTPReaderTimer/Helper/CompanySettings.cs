using System;
using System.Collections.Generic;

namespace SFTPReaderTimer.Helper
{
    public class CompanySettings
    {
        public static string FolderPath => Environment.GetEnvironmentVariable("FolderPath");
        public static string QueueName => Environment.GetEnvironmentVariable("QueueName");
        public static string Host => Environment.GetEnvironmentVariable("SFTPServer");
        public static int Port => int.Parse(Environment.GetEnvironmentVariable("SFTPPort"));
        public static string Username => Environment.GetEnvironmentVariable("SFTPUsername");
        public static string Password => Environment.GetEnvironmentVariable("SFTPPassword");
        public static string[] Branches => Environment.GetEnvironmentVariable("Branches")?.Split(',');
        public static int Month
        {
            get
            {
                string monthString = Environment.GetEnvironmentVariable("Month");
                int month = int.Parse(monthString);
                return month;
            }
        }
        public static List<string> Headers => new List<string> { "Date", "File Name", "Transaction ID", "Transaction Details", "Business Central Response", "Status" };
    }

}
