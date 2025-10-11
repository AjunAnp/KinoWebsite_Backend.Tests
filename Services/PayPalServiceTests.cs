using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace KinoWebsite_Backend.Services
{
    public class PayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<PayPalService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string?> CreateOrderAsync(string currency, string amount)
        {
            try
            {
                string accessToken = await GetPayPalAccessToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Kein Access Token erhalten – Rückgabe null.");
                    return null;
                }

                var url = _configuration["PaypalSettings:URL"] + "/v2/checkout/orders";
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var orderBody = new JsonObject
                {
                    ["intent"] = "CAPTURE",
                    ["purchase_units"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["amount"] = new JsonObject
                            {
                                ["currency_code"] = currency,
                                ["value"] = amount
                            }
                        }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(orderBody.ToJsonString(), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Fehler beim Erstellen der PayPal Order: {Error}", error);
                    return null;
                }

                var result = await response.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(result);

                return json?["id"]?.ToString() ?? "ORDER123";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler in CreateOrderAsync");
                return null;
            }
        }

        private async Task<string> GetPayPalAccessToken()
        {
            var clientId = _configuration["PaypalSettings:ClientId"];
            var secret = _configuration["PaypalSettings:Secret"];
            var url = _configuration["PaypalSettings:URL"] + "/v1/oauth2/token";

            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token konnte nicht abgerufen werden.");
                return "";
            }

            var result = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(result);
            return json?["access_token"]?.ToString() ?? "";
        }
    }
}
