using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using SFTPReaderTimer.Services;
using System;

[assembly: FunctionsStartup(typeof(SFTPReaderTimer.Startup))]
namespace SFTPReaderTimer
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            try
            {
                builder.Services.AddSingleton(new SftpService());
                builder.Services.AddSingleton(x => CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage")).CreateCloudQueueClient());
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during configuration
                Console.WriteLine($"Error during configuration: {ex.Message}");
                throw;
            }
        }
    }
}
