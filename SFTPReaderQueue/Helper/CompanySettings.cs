using System;
using System.Collections.Generic;

namespace SFTPReaderQueue.Helper
{
    public class CompanySettings
    {
        public static string Host => Environment.GetEnvironmentVariable("SFTPServer");
        public static int Port => int.Parse(Environment.GetEnvironmentVariable("SFTPPort"));
        public static string Username => Environment.GetEnvironmentVariable("SFTPUsername");
        public static string Password => Environment.GetEnvironmentVariable("SFTPPassword");
        public static string Authority => Environment.GetEnvironmentVariable("Authority");
        public static string ClientID => Environment.GetEnvironmentVariable("ClientID");
        public static string ClientSecret => Environment.GetEnvironmentVariable("ClientSecret");
        public static string Resource => Environment.GetEnvironmentVariable("Resource");
        public static string BC_CreateSI_Endpoint => Environment.GetEnvironmentVariable("BC_CreateSI_Endpoint");
        public static string BC_CreateCM_Endpoint => Environment.GetEnvironmentVariable("BC_CreateCM_Endpoint");
        public static List<string> Headers => new List<string> { "Date", "File Name", "Transaction ID", "Transaction Details", "Business Central Response", "Status" };
    }

}
