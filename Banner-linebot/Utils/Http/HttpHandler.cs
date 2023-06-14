using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Banner.LineBot.Utils.Http
{
    internal sealed class HttpHandler : IHttpHandler
    {
        private HttpClient HttpClient = new HttpClient();

        public async Task<HttpResponseMessage> PostAsync(Uri url, string requestBodyJson)
        {
            SetAcceptJson();
            return await HttpClient.PostAsync(url,
                new StringContent(requestBodyJson, Encoding.UTF8, "application/json"));
        }

        public void SetBearer(string token)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private void SetAcceptJson()
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}