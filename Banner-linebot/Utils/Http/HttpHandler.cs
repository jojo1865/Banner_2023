using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Banner.LineBot.Utils.Http
{
    public class HttpHandler : IHttpHandler
    {
        private readonly HttpClient _client = new HttpClient();
        
        public async Task<HttpResponseMessage> PostAsync(Uri url, string requestBodyJson)
        {
            return await _client.PostAsync(url, new StringContent(requestBodyJson, Encoding.UTF8, "application/json"));
        }

        public void SetFormatAsJson()
        {
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetBearer(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}