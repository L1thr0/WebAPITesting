using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NUnitTests.Auth
{
    public static class TokenProvider
    {
        public static async Task<string> GetTokenAsync()
        {
            using var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://login.microsoftonline.com/d0931c35-94f4-49e6-8587-c0715af471f3/oauth2/v2.0/token");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_Id", "a8d2586b-e933-4623-b654-736e4a5d4d5e"),
                new KeyValuePair<string, string>("client_Secret", "Knb8Q~poM-r_T~geHJkoDAflxpI5D6NZ3F~IJcDt"),
                new KeyValuePair<string, string>("scope", "api://598eeaa3-cb83-43a0-9980-15c0da758bab/.default"),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            request.Content = content;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
    }
}
