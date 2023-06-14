using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

        public async Task<HttpResponseMessage> GetAsync(Uri url)
        {
            SetAcceptJson();
            return await HttpClient.GetAsync(url);
        }

        public async Task<HttpResponseMessage> GetAsync<T>(Uri url, T query)
        {
            if (query == null)
                return await GetAsync(url);

            var uriBuilder = new UriBuilder(url);
            var queryToEdit = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (PropertyInfo propertyInfo in query.GetType().GetProperties())
            {
                queryToEdit[propertyInfo.Name] = propertyInfo.GetValue(query).ToString();
            }

            uriBuilder.Query = queryToEdit.ToString();
            Uri uriWithQueryString = new Uri(uriBuilder.ToString());

            return await GetAsync(uriWithQueryString);
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