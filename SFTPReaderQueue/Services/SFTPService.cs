using Microsoft.Extensions.Logging;
using Renci.SshNet;
using SFTPReaderQueue.Helper;
using SFTPReaderQueue.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SFTPReaderQueue.Services
{
    public class SftpService : CompanySettings
    {
        // Static column indices for search keywords
        private static int TransactionIDColumn = 2;
        private static string Status = "Processing";

        public void UpdateCsv(string filePath, string TransactionID, string BCResponse, string StatusUpdate, ILogger log)
        {
            // Connect to the SFTP server
            using (var sftpClient = new SftpClient(Host, Port, Username, Password))
            {
                sftpClient.Connect();

                // Read the CSV file from the SFTP server and store its content in memory
                List<string[]> csvRows = new List<string[]>();
                using (var reader = new StreamReader(sftpClient.OpenRead(filePath)))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] cells = line.Split(',');
                        csvRows.Add(cells);
                    }
                }

                // Search for the two keywords in the specified columns and update the target column if both keywords are found
                bool keywordsFound = false;
                for (int rowIndex = 1; rowIndex < csvRows.Count; rowIndex++) // Start from index 1 to skip the header row
                {
                    string[] cells = csvRows[rowIndex];
                    int StatusColumn = cells.Length - 1;
                    int BCResponseColumn = cells.Length - 2;

                    if (cells.Length > TransactionIDColumn && cells.Length > StatusColumn &&
                        cells[TransactionIDColumn].Contains(TransactionID) && cells[StatusColumn].Contains(Status))
                    {
                        keywordsFound = true;

                        // Update the value in the BC Response and Status Columns
                        cells[BCResponseColumn] = $"\"{BCResponse.Replace("\"", "\"\"")}\"";
                        cells[StatusColumn] = StatusUpdate;
                        csvRows[rowIndex] = cells;

                        // Exit the loop once the keyword is found
                        break;
                    }
                }

                // Check if both keywords were found
                if (!keywordsFound)
                {
                    log.LogError($"Transaction ID: '{TransactionID}' not found.");
                    return;
                }

                // Write the modified content back to the CSV file on the SFTP server
                using (var writer = new StreamWriter(sftpClient.OpenWrite(filePath)))
                {
                    foreach (string[] cells in csvRows)
                    {
                        string line = string.Join(",", cells);
                        writer.WriteLine(line);
                    }
                }

                log.LogInformation($"Successfully updated Transaction ID: {TransactionID}");
                sftpClient.Disconnect();
            }
        }

        public bool ReplaceTextInFile(string filePath, string searchKeyword, string newText, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();
                    var fileContent = client.ReadAllText(filePath);
                    var regex = new Regex(searchKeyword);
                    fileContent = regex.Replace(fileContent, newText);
                    client.WriteAllText(filePath, fileContent);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error replacing text in file: {ex.Message}");
                return false;
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

        public void CreateTextFile(string filePath, string content, ILogger log)
        {
            try
            {
                using (var client = new SftpClient(Host, Port, Username, Password))
                {
                    client.Connect();
                    using (var stream = client.AppendText(filePath))
                    {
                        stream.WriteLine(content);
                    }
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                // handle the exception here, e.g. log the error
                log.LogError($"Error creating text file on SFTP server: {ex.Message}");
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