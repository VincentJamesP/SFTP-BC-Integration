using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SFTPReaderRetry.Helper;

namespace SFTPReaderRetry.Functions
{
    public class TimerTriggerFunction : CompanySettings
    {
        private readonly SftpService _sftpService;
        private string _archivePath;
        private string _failedArchivePath;
        private string _logPath;
        private string _failedTxnPath;
        private string _rootPath;
        private string _toBeDeletedPath;

        public TimerTriggerFunction(SftpService sftpService)
        {
            _sftpService = sftpService;
        }

        [FunctionName("TimerTriggerFunction")]
        public void Run([TimerTrigger("0 0 10 * * *")] TimerInfo myTimer, ILogger log)
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

        private void SetDirectories(string rootDir)
        {
            _rootPath = rootDir;
            _archivePath = Path.Combine(rootDir, "archive");
            _failedArchivePath = Path.Combine(_archivePath + "/", "failed");
            _logPath = Path.Combine(rootDir, "logs");
            _failedTxnPath = Path.Combine(_logPath + "/", "failed");
            _toBeDeletedPath = Path.Combine(_failedTxnPath + "/", "To be deleted");
        }

        private bool CreateDirectories(ILogger log)
        {
            if (!_sftpService.CreateDirectoryIfNotExists(_toBeDeletedPath, log))
            {
                log.LogError($"To be deleted folder does not exist: {_toBeDeletedPath}");
                return false;
            }

            return true;
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

                if (!_sftpService.CheckSftpFolderExists(_failedArchivePath, log))
                {
                    log.LogError($"Failed archive folder does not exist: {_failedArchivePath}");
                    return;
                }

                if (!_sftpService.CheckSftpFolderExists(_failedTxnPath, log))
                {
                    log.LogError($"Failed logs folder does not exist: {_failedTxnPath}");
                    return;
                }

                if (!CreateDirectories(log))
                {
                    return;
                }

                MoveFailedTransactions(log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        private void MoveFailedTransactions(ILogger log)
        {
            try
            {
                // Move failed transactions to branch folder
                bool IsTransferSuccessful = _sftpService.MoveFiles(_failedArchivePath, _rootPath, log);

                if (IsTransferSuccessful)
                {
                    log.LogInformation($"Failed transaction files were transferred succesfully from {_failedArchivePath} to {_rootPath}.");

                    // Move log files to To be deleted folder
                    bool isLogTransferSuccessful = _sftpService.MoveFiles(_failedTxnPath, _toBeDeletedPath, log);

                    if (isLogTransferSuccessful)
                    {
                        log.LogInformation($"Failed log files were transferred succesfully from {_failedTxnPath} to {_toBeDeletedPath}.");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred while moving the files: {ex.Message}");
            }
        }
    }
}
