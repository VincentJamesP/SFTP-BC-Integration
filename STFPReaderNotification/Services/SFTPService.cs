using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using SFTPReaderNotification.Helper;
using System;
using System.Collections.Generic;

public class SftpService : CompanySettings
{
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

}
