using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using SFTPReaderTimer.Helper;
using SFTPReaderTimer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace SFTPReaderTimer.Services
{
    public class SftpService : CompanySettings
    {
        public List<SftpFile> ListFiles(string directoryPath, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();
                    var files = client.ListDirectory(directoryPath);
                    var sftpFiles = new List<SftpFile>();
                    foreach (var file in files)
                    {
                        if (!file.IsDirectory)
                        {
                            sftpFiles.Add(file);
                        }
                    }
                    return sftpFiles;
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error listing files on SFTP server: {ex.Message}");
                return null;
            }
        }

        public List<SftpFile> ListDirectories(string directoryPath, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();
                    var files = client.ListDirectory(directoryPath);
                    var sftpDirectories = new List<SftpFile>();
                    foreach (var file in files)
                    {
                        if (file.IsDirectory && file.Name != "." && file.Name != "..")
                        {
                            sftpDirectories.Add(file);
                        }
                    }
                    return sftpDirectories;
                }
            }
            catch (Exception ex)
            {
                // Handle the exception here, e.g. log the error
                log.LogError($"Error listing directories on SFTP server: {ex.Message}");
                return null;
            }
        }



        public string ReadFile(string filePath, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();
                    using (var stream = new MemoryStream())
                    {
                        client.DownloadFile(filePath, stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error reading file from SFTP server: {ex.Message}");
                return null;
            }
        }
        public void WriteToCsv(string filePath, List<string> headers, List<LogContent> rowDataList, ILogger log)
        {
            // Check if the CSV file already exists on the SFTP server
            bool fileExists = false;
            using (var client = new SftpClient(Host, Port, Username, Password))
            {
                client.Connect();

                try
                {
                    fileExists = client.Exists(filePath);
                }
                catch (Exception ex)
                {
                    log.LogError($"Error checking existence of file from SFTP server: {ex.Message}");
                }
                finally
                {
                    client.Disconnect();
                }
            }

            // Open the CSV file on the SFTP server in append mode
            using (var client = new SftpClient(Host, Port, Username, Password))
            {
                client.Connect();

                // Write headers if the file is newly created or doesn't exist
                if (!fileExists)
                {
                    string headerLine = string.Join(",", headers);
                    using (var stream = client.AppendText(filePath))
                    {
                        stream.WriteLine(headerLine);
                    }
                }

                // Create a list to hold the rows of data
                List<string> rows = new List<string>();

                // Write the new row data
                foreach (LogContent logData in rowDataList)
                {
                    // Create a data row
                    string dataRow = string.Join(",",
                        logData.DateTime,
                        logData.FileName,
                        logData.TransactionID,
                        $"\"{logData.TransactionDetails.Replace("\"", "\"\"")}\"",
                        $"\"{logData.BCResponse.Replace("\"", "\"\"")}\"",
                        logData.Status);

                    rows.Add(dataRow);
                }

                using (var stream = client.AppendText(filePath))
                {
                    foreach (string row in rows)
                    {
                        stream.WriteLine(row);
                    }
                }

                client.Disconnect();
            }
        }

        public bool CheckSftpFolderExists(string folderPath, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();

                    if (!client.Exists(folderPath))
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error checking SFTP folder: {ex.Message}");
                return false;
            }
        }

        public bool CreateDirectoryIfNotExists(string folderPath, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();

                    // check if the directory exists
                    if (!client.Exists(folderPath))
                    {
                        // create the directory
                        client.CreateDirectory(folderPath);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error creating SFTP folder: {ex.Message}");
                return false;
            }
        }

        public bool MoveFile(string sourceFilePath, string destinationFolderPath, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();

                    // get the file name from the source file path
                    var fileName = Path.GetFileName(sourceFilePath);

                    // get the destination file path by combining the destination folder path and the file name
                    var destinationFilePath = $"{destinationFolderPath}/{fileName}";

                    // move the file to the destination folder
                    client.RenameFile(sourceFilePath, destinationFilePath);

                    return true;
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error moving file on SFTP server: {ex.Message}");
                return false;
            }
        }

    }
}