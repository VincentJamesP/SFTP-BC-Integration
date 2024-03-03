namespace SFTPReaderQueue.Models
{
    public class LogContent
    {
        public string DateTime { get; set; }
        public string FileName { get; set; }
        public string TransactionID { get; set; }
        public string Status { get; set; }
        public string BCResponse { get; set; }
        public string TransactionDetails { get; set; }
    }
}
