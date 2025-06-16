using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CurrencyConverter.Tests.Common
{
    public static class TestAuthHelper
    {
        public static async Task AddJwtTokenAsync(HttpClient client, TestTokenRequest testTokenRequest)
        {

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/dev/token")
            {
                Content = new StringContent(JsonSerializer.Serialize(testTokenRequest), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadAsStringAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public class TestTokenRequest
        {
            public string? Username { get; set; } = "test-user";
            public Dictionary<string, bool> Permissions { get; set; } = new();
            public int ExpirationInSeconds { get; set; } = 15 * 60;
        }
    }
}
