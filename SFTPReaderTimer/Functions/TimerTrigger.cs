using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using SFTPReaderTimer.Helper;
using System.Linq;
using SFTPReaderTimer.Models;
using System.Globalization;
using SFTPReaderTimer.Services;

namespace SFTPReaderTimer.Functions
{
    public class TimerTriggerFunction : CompanySettings
    {
        private readonly SftpService _sftpService;
        private readonly CloudQueue _queue;
        private string _archivePath;
        private string _failedArchivePath;
        private string _logPath;
        private string _blockedTxnPath;
        private string _voidTxnPath;
        private string _failedTxnPath;
        private readonly DateTime _philippineDateNow;
        private string _rootPath;

        public TimerTriggerFunction(SftpService sftpService, CloudQueueClient queueClient)
        {
            _sftpService = sftpService;
            _queue = queueClient.GetQueueReference(QueueName);
            _queue.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            _philippineDateNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Utc).AddHours(8);
        }

        [FunctionName("TimerTriggerFunction")]
        public void Run([TimerTrigger("0 */5 10-21 * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                GetBranchRoot(log);
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred: {ex.Message}");
            }
        }

        private void GetBranchRoot(ILogger log)
        {
            try
            {
                // Retrieve branches
                var branches = _sftpService.ListDirectories(FolderPath, log);
                foreach (var branch in branches)
                {
                    // Set root path
                    if (IsBranchInRange(branch.Name))
                    {
                        var currentRoot = branch.FullName + "/";
                        SetDirectories(currentRoot);

                        // Process files
                        Process(log);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred while retrieving root folder: {ex.Message}");
            }
        }

        private void SetDirectories(string rootDir)
        {
            _rootPath = rootDir;
            _archivePath = Path.Combine(rootDir, "archive");
            _failedArchivePath = Path.Combine(_archivePath + "/", "failed");
            _logPath = Path.Combine(rootDir, "logs");
            _blockedTxnPath = Path.Combine(_logPath + "/", "blocked");
            _voidTxnPath = Path.Combine(_logPath + "/", "void");
            _failedTxnPath = Path.Combine(_logPath + "/", "failed");
        }

        private void Process(ILogger log)
        {
            try
            {
                if (!_sftpService.CheckSftpFolderExists(_rootPath, log))
                {
                    log.LogError($"Transaction folder does not exist: {_rootPath}");
                    return;
                }

                if (!CreateDirectories(log))
                {
                    return;
                }

                MapData(log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        private bool CreateDirectories(ILogger log)
        {

            if (!_sftpService.CreateDirectoryIfNotExists(_archivePath, log))
            {
                log.LogError($"Archive folder does not exist: {_archivePath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_failedArchivePath, log))
            {
                log.LogError($"Failed archive folder does not exist: {_failedArchivePath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_logPath, log))
            {
                log.LogError($"Log folder does not exist: {_logPath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_blockedTxnPath, log))
            {
                log.LogError($"Blocked log folder does not exist: {_blockedTxnPath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_voidTxnPath, log))
            {
                log.LogError($"Voided log folder does not exist: {_voidTxnPath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_failedTxnPath, log))
            {
                log.LogError($"Failed log folder does not exist: {_failedTxnPath}");
                return false;
            }

            return true;
        }

        private string GetLogFileName()
        {
            return $"{_logPath}/SFTPReader-log-{_philippineDateNow:yyyy-MM-dd}.csv";
        }

        private string GetFailedFileName()
        {
            return $"{_failedTxnPath}/SFTPReader-error-log-{_philippineDateNow:yyyy-MM-dd}.csv";
        }

        private string GetVoidedFileName()
        {
            return $"{_voidTxnPath}/SFTPReader-voided-log-{_philippineDateNow:yyyy-MM-dd}.csv";
        }

        private string GetBlockedFileName()
        {
            return $"{_blockedTxnPath}/SFTPReader-blocked-log-{_philippineDateNow:yyyy-MM-dd}.csv";
        }

        private void MapData(ILogger log)
        {
            var sftpFiles = _sftpService.ListFiles(_rootPath, log)
                    .Where(file => file.Name.ToLower().EndsWith(".json"))
                    .ToList();

            foreach (var file in sftpFiles)
            {
                // SFTP file service
                var fileContents = _sftpService.ReadFile(file.FullName, log);

                // Deserialize the order data into a dynamic object
                dynamic orderData = JsonConvert.DeserializeObject(fileContents);

                // Check date of transaction
                Status status = JsonConvert.DeserializeObject<Status>(orderData.stat.ToString());

                if (!IsMonthInRange(status.sysdate))
                {
                    continue;
                }

                // Deserialize the order data into specified models
                Header header = JsonConvert.DeserializeObject<Header>(orderData.hdr.ToString());
                List<Detail> details = JsonConvert.DeserializeObject<List<Detail>>(orderData.detail.ToString());
                List<Payment> payments = JsonConvert.DeserializeObject<List<Payment>>(orderData.payment.ToString());

                // Map Invoice Header
                OrderDetails orderDetails = new OrderDetails();
                orderDetails.InvoiceHeader = new List<InvoiceHeader>
                {
                    new InvoiceHeader()
                    {
                        CustNo = header.branch_cd,
                        KtiSourceSalesOrderid = header.sys_trans_num,
                        Customername = $"{header.cd_first_name} {header.cd_last_name}",
                        Address = header.cd_addr,
                        Phoneno = header.cd_mobilenumber
                    }
                };

                string transactionID = header.sys_trans_num;

                // Map Invoice Line
                orderDetails.InvoiceLine = new List<InvoiceLine>();
                foreach (Detail detail in details)
                {
                    orderDetails.InvoiceLine.Add(new InvoiceLine()
                    {
                        KtiSourceSalesOrderid = detail.sys_trans_num,
                        ItemNo = detail.item_code,
                        VariantCode = "",
                        Quantity = detail.qty,
                        UnitofMeasureCode = detail.sell_uom,
                        KtiSourcesalesorderitemid = $"{detail.sys_trans_num}-{detail.seq_num}",
                        UnitCost = detail.cost,
                        Price = detail.net_retail
                    });
                }

                // Map Payment Method
                orderDetails.PaymentMethod = new List<PaymentMethod>();
                foreach (Payment payment in payments)
                {
                    var tenderType = new TenderType()
                    {
                        TRN = payment.ref_num,
                        Code = payment.tender_type_code,
                        Amount = int.Parse(payment.conv_pay_amount_due) - int.Parse(payment.conv_change_amount)
                    };

                    var paymentMethod = new PaymentMethod()
                    {
                        KtiSourceSalesOrderid = payment.sys_trans_num,
                        TenderType = new List<TenderType> { tenderType }
                    };

                    orderDetails.PaymentMethod.Add(paymentMethod);
                }

                if (LogStatus(file.Name, transactionID, orderDetails, log))
                {
                    MoveFile(file.FullName, _archivePath, log);
                }
            }
        }

        private bool LogStatus(string filename, string transactionID, OrderDetails orderDetails, ILogger log)
        {
            try
            {
                DateTime _philippineDateNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Utc).AddHours(8);
                var logFileName = GetLogFileName();
                var failedFileName = GetFailedFileName();
                var voidedFileName = GetVoidedFileName();
                var blockedFileName = GetBlockedFileName();
                var status = "Processing";

                if (!filename.ToLower().Contains("sv"))
                {
                    OrderAndFileDetails orderAndFileDetails = new OrderAndFileDetails();
                    orderAndFileDetails.Root = _rootPath;
                    orderAndFileDetails.Filename = filename;
                    orderAndFileDetails.OrderDetails = orderDetails;

                    // Serialize the orderDetails object to JSON
                    string orderDetailsJson = JsonConvert.SerializeObject(orderAndFileDetails);
                    string base64OrderDetails = Base64Helper.EncodeToBase64(orderDetailsJson);

                    // Create a new message and add it to the queue
                    CloudQueueMessage message = new CloudQueueMessage(base64OrderDetails);
                    _queue.AddMessageAsync(message).GetAwaiter().GetResult();

                    log.LogInformation($"Message added to queue: {orderDetailsJson}");
                }
                else
                {
                    status = "Blocked";
                }

                List<LogContent> logData = new List<LogContent>
                {
                    new LogContent
                    {
                        DateTime = $"{_philippineDateNow:MM/dd/yyyy hh:mm:ss tt}",
                        FileName = filename,
                        TransactionID = transactionID,
                        TransactionDetails = JsonConvert.SerializeObject(orderDetails),
                        BCResponse = "",
                        Status = status
                    }
                };

                if (filename.ToLower().Contains("sv"))
                {
                    _sftpService.WriteToCsv(blockedFileName, Headers, logData, log);
                }
                else if (filename.ToLower().Contains("v"))
                {
                    _sftpService.WriteToCsv(voidedFileName, Headers, logData, log);
                }

                _sftpService.WriteToCsv(logFileName, Headers, logData, log);

                return true;
            }

            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                return false;
            }
        }

        private void MoveFile(string filename, string destinationFolder, ILogger log)
        {
            if (!_sftpService.MoveFile(filename, destinationFolder, log))
            {
                log.LogError($"Error moving file on SFTP server: {filename}");
                return;
            }
        }

        private bool IsMonthInRange(string sysdate)
        {
            try
            {
                DateTime date = DateTime.ParseExact(sysdate, "yyyyMMdd", CultureInfo.InvariantCulture);
                return date.Month == Month;
            }
            catch (FormatException)
            {
                // Handle invalid date format
                return false;
            }
        }

        private bool IsBranchInRange(string branchName)
        {
            foreach (string branch in Branches)
            {
                if (branch.ToLower().Equals(branchName.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}