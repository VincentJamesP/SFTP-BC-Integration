using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using SFTPReaderRetry.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class SftpService : CompanySettings
{
    public bool MoveFiles(string sourceFolderPath, string destinationFolderPath, ILogger log)
    {
        try
        {
            using (var client = new SftpClient(SFTPServer, SFTPPort, SFTPUsername, SFTPPassword))
            {
                client.Connect();

                // Get a list of files in the source folder
                var files = client.ListDirectory(sourceFolderPath);

                // Move each file to the destination folder
                foreach (var file in files)
                {
                    if (!file.IsDirectory)
                    {
                        // Get the file name
                        var fileName = file.Name;

                        // Get the destination file path by combining the destination folder path and the file name
                        var destinationFilePath = destinationFolderPath.EndsWith("/") ? $"{destinationFolderPath}{fileName}" : $"{destinationFolderPath}/{fileName}";

                        // Move the file to the destination folder
                        client.RenameFile($"{sourceFolderPath}/{fileName}", destinationFilePath);
                    }
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            // handle the exception here, e.g. log the error
            log.LogError($"Error moving files on SFTP server: {ex.Message}");
            return false;
        }
    }

    public List<SftpFile> ListDirectories(string directoryPath, ILogger log)
    {
        try
        {
            using (var client = new SftpClient(SFTPServer, SFTPPort, SFTPUsername, SFTPPassword))
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

    public bool CheckSftpFolderExists(string folderPath, ILogger log)
    {
        try
        {
            using (var client = new SftpClient(SFTPServer, SFTPPort, SFTPUsername, SFTPPassword))
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
            using (var client = new SftpClient(SFTPServer, SFTPPort, SFTPUsername, SFTPPassword))
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

}
