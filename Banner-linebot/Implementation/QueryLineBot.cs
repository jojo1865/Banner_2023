using System;
using System.Net.Http;
using System.Threading.Tasks;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Models;
using Banner.LineBot.Utils.Http;
using Banner.LineBot.Variables;
using Newtonsoft.Json;

namespace Banner.LineBot.Implementation
{
    /// <inheritdoc />
    internal class QueryLineBot : IQueryLineBot
    {
        private readonly IHttpHandler _httpHandler;

        public QueryLineBot(string channelAccessToken, IHttpHandler httpHandler = null)
        {
            _httpHandler = httpHandler ?? new HttpHandler();
            _httpHandler.SetBearer(channelAccessToken);
        }

        /// <inheritdoc />
        public async Task<int> GetQuota()
        {
            string body = await GetWithoutQuery(LineApiEndpoints.QuotaUri);
            QuotaResponse responseObject = JsonConvert.DeserializeObject<QuotaResponse>(body);

            return responseObject?.value ?? 0;
        }

        /// <inheritdoc />
        public async Task<int> GetSentCountThisMonth()
        {
            string body = await GetWithoutQuery(LineApiEndpoints.QuotaConsumptionUri);
            ConsumptionResponse responseObject = JsonConvert.DeserializeObject<ConsumptionResponse>(body);

            return responseObject?.TotalUsage ?? 0;
        }

        private async Task<string> GetWithoutQuery(Uri uri)
        {
            HttpResponseMessage response = await _httpHandler.GetAsync(uri);
            string body = await response.Content.ReadAsStringAsync();
            return body;
        }
    }
}