using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.DependencyInjection;
using SFTPReaderQueue.Services;
using System;

[assembly: FunctionsStartup(typeof(SFTPReaderQueue.Startup))]

namespace SFTPReaderQueue
{
    public class Startup : FunctionsStartup
    {
        private readonly string AzureWebJobsStorage = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        public override void Configure(IFunctionsHostBuilder builder)
        {
            try
            {
                builder.Services.AddSingleton(new SftpService());
                builder.Services.AddOptions<ExecutionContextOptions>()
                    .Configure(options =>
                    {
                        options.AppDirectory = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
                        Environment.SetEnvironmentVariable("AzureWebJobsStorage", AzureWebJobsStorage);
                    });

                builder.Services.AddSingleton<BusinessCentralClient>();
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
