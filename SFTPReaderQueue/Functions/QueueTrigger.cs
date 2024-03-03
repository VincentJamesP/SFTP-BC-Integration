using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SFTPReaderQueue.Helper;
using SFTPReaderQueue.Models;
using SFTPReaderQueue.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SFTPReaderQueue.Functions
{
    public class QueueTriggerFunction : CompanySettings
    {
        private readonly BusinessCentralClient _bcClient;
        private readonly SftpService _sftpService;
        private string _archivePath;
        private string _failedArchivePath;
        private string _logPath;
        private string _voidTxnPath;
        private string _failedTxnPath;
        private string _rootPath;
        private readonly DateTime _philippineDateNow;

        public QueueTriggerFunction(SftpService sftpService, BusinessCentralClient bcClient)
        {
            _sftpService = sftpService;
            _bcClient = bcClient;
            _philippineDateNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Utc).AddHours(8);
        }

        [FunctionName("QueueTriggerFunction")]
        public async Task Run([QueueTrigger("order-details")] string myQueueItem, ILogger log)
        {
            try
            {
                string orderDetail = Base64Helper.DecodeFromBase64(myQueueItem);

                log.LogInformation($"C# Queue trigger function processed: {orderDetail}");

                var orderAndFileDetails = JsonConvert.DeserializeObject<OrderAndFileDetails>(orderDetail);

                SetDirectories(orderAndFileDetails.Root);
                await ProcessQueueBatchAsync(orderAndFileDetails, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        private void SetDirectories(string rootDir)
        {
            _rootPath = rootDir;
            _archivePath = Path.Combine(rootDir, "archive");
            _failedArchivePath = Path.Combine(_archivePath + "/", "failed");
            _logPath = Path.Combine(rootDir, "logs");
            _voidTxnPath = Path.Combine(_logPath + "/", "void");
            _failedTxnPath = Path.Combine(_logPath + "/", "failed");
        }

        private async Task ProcessQueueBatchAsync(OrderAndFileDetails order, ILogger log)
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

                await ProcessTransactionAsync(order, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
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

        private string GetArchivedFileName(string filename)
        {
            return $"{_archivePath}/{filename}";
        }

        private bool CreateDirectories(ILogger log)
        {
            if (!_sftpService.CreateDirectoryIfNotExists(_archivePath, log))
            {
                log.LogError($"Archive folder does not exist: {_archivePath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_logPath, log))
            {
                log.LogError($"Log folder does not exist: {_logPath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_failedTxnPath, log))
            {
                log.LogError($"Failed log folder does not exist: {_failedTxnPath}");
                return false;
            }

            if (!_sftpService.CreateDirectoryIfNotExists(_voidTxnPath, log))
            {
                log.LogError($"Voided log folder does not exist: {_voidTxnPath}");
                return false;
            }

            return true;
        }

        private async Task ProcessTransactionAsync(OrderAndFileDetails order, ILogger log)
        {
            try
            {
                var filename = order.Filename;
                var response = await _bcClient.CallBusinessCentralAsync(order.Filename.ToLower().Contains("v") ? BC_CreateCM_Endpoint : BC_CreateSI_Endpoint, new JSONData { jsonData = JsonConvert.SerializeObject(order.OrderDetails) });
                //StatusUpdate(order.OrderDetails.InvoiceHeader[0].KtiSourceSalesOrderid, response, filename, log);
                StatusUpdate(order.OrderDetails, response, filename, log);
                //test(response, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        private void test(HttpResponseMessage response, ILogger log)
        {
            string logFileName = GetLogFileName();
            string trnID = "001-000026-1681447794-1";
            string newStatus = "test";
            var result = response.Content.ReadAsStringAsync().Result;
            var odataContent = ODataHelper.ExtractValueObject(result);
            _sftpService.UpdateCsv(logFileName, trnID, odataContent, newStatus, log);
        }

        private void StatusUpdate(OrderDetails order, HttpResponseMessage response, string filename, ILogger log)
        {
            var logFileName = GetLogFileName();
            var failedFileName = GetFailedFileName();
            var voidedFileName = GetVoidedFileName();
            var archivedFileName = GetArchivedFileName(filename);

            string transactionID = order.InvoiceHeader[0].KtiSourceSalesOrderid;
            var result = response.Content.ReadAsStringAsync().Result;
            string status = response.IsSuccessStatusCode ? "Success" : "Failed";

            if (response.IsSuccessStatusCode)
            {
                result = ODataHelper.ExtractValueObject(result);
            }
            else
            {
                log.LogError($"Business Central error: {result}");
                _sftpService.MoveFile(archivedFileName, _failedArchivePath, log);

                List<LogContent> logData = new List<LogContent>
                {
                    new LogContent
                    {
                        DateTime = $"{_philippineDateNow:MM/dd/yyyy hh:mm:ss tt}",
                        FileName = filename,
                        TransactionID = transactionID,
                        TransactionDetails = JsonConvert.SerializeObject(order),
                        BCResponse = result,
                        Status = status
                    }
                };

                _sftpService.WriteToCsv(failedFileName, Headers, logData, log);
            }

            if (filename.ToLower().Contains("v"))
            {
                status = "Void";
                _sftpService.UpdateCsv(voidedFileName, transactionID, result, status, log);
            }

            _sftpService.UpdateCsv(logFileName, transactionID, result, status, log);
        }

    }

}