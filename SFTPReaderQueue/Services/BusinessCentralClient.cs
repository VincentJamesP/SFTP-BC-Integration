using Microsoft.Identity.Client;
using Newtonsoft.Json;
using SFTPReaderQueue.Helper;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SFTPReaderQueue.Services
{
    public class BusinessCentralClient : CompanySettings
    {
        private readonly IConfidentialClientApplication _app;

        public BusinessCentralClient()
        {
            try
            {
                _app = ConfidentialClientApplicationBuilder.Create(ClientID)
                            .WithAuthority(new Uri(Authority))
                            .WithClientSecret(ClientSecret)
                            .Build();
            }
            catch (Exception ex)
            {
                // handle the exception here
                throw new InvalidOperationException("Failed to create BusinessCentralClient.", ex);
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var scopes = new[] { $"{Resource}/.default" };

            try
            {
                var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Handle the exception as required
                Console.WriteLine($"An error occurred while acquiring access token: {ex.Message}");
                throw;
            }
        }

        public async Task<HttpResponseMessage> CallBusinessCentralAsync<T>(string endpoint, T content)
        {
            var accessToken = await GetAccessTokenAsync();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var httpContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, httpContent);
                return response;
            }
            catch (Exception ex)
            {
                // Handle the exception as required
                Console.WriteLine($"An error occurred while calling Business Central: {ex.Message}");
                throw;
            }
        }
    }

}