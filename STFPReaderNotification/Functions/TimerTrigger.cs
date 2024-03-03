using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Renci.SshNet;
using System.IO;
using SFTPReaderNotification.Helper;

namespace SFTPReaderNotification.Functions
{
    public class TimerTriggerFunction : CompanySettings
    {
        private readonly SftpService _sftpService;
        private string _logPath;
        private string _failedTxnPath;
        private string _rootPath;

        public TimerTriggerFunction(SftpService sftpService)
        {
            _sftpService = sftpService;
        }

        [FunctionName("TimerTriggerFunction")]
        public void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, ILogger log)
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
            _logPath = Path.Combine(rootDir, "logs");
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

                SendEmail(log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
        }

        private void SendEmail(ILogger log)
        { 
            try
            {
                // Connect to the SFTP server and download the file
                using (var sftpClient = new SftpClient(SFTPServer, SFTPPort, SFTPUsername, SFTPPassword))
                {
                    sftpClient.Connect();

                    // Check if the folder exists on the SFTP server
                    if (!sftpClient.Exists(_failedTxnPath))
                    {
                        log.LogError($"The specified folder '{_failedTxnPath}' does not exist on the SFTP server.");
                        return; // Exit
                    }

                    var logFiles = sftpClient.ListDirectory(_failedTxnPath);

                    using (SmtpClient smtpClient = new SmtpClient(SMTPServer, SMTPPort))
                    {
                        smtpClient.EnableSsl = true;
                        smtpClient.Credentials = new NetworkCredential(SenderEmail, SenderPassword);

                        using (MailMessage mailMessage = new MailMessage())
                        {
                            mailMessage.From = new MailAddress(SenderEmail);
                            mailMessage.To.Add(RecipientEmail);
                            mailMessage.Subject = EmailSubject;
                            mailMessage.Body = EmailBody;

                            foreach (var logFile in logFiles)
                            {
                                if (!logFile.IsDirectory) // Ignore directories
                                {
                                    MemoryStream stream = new MemoryStream();
                                    // Download each file
                                    string remoteFilePath = _failedTxnPath + "/" + logFile.Name;
                                    sftpClient.DownloadFile(remoteFilePath, stream);
                                    stream.Position = 0;

                                    // Add each downloaded file as an attachment to the email
                                    mailMessage.Attachments.Add(new Attachment(stream, logFile.Name));
                                    stream.Position = 0; // Reset the stream position for the next attachment

                                    log.LogInformation($"Added attachment: {logFile.Name}");
                                }
                            }

                            // Send the email with all the attachments
                            smtpClient.Send(mailMessage);

                            foreach (var attachment in mailMessage.Attachments)
                            {
                                attachment.Dispose();
                            }

                            log.LogInformation($"Email sent successfully to {RecipientEmail}.");
                        }
                    }
                    sftpClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred while sending the email: {ex.Message}");
            }
        }
    }
}