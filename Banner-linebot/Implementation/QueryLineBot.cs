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
        public async Task<int> GetQuotaAsync()
        {
            string body = await GetWithoutQuery(LineApiEndpoints.QuotaUri);
            QuotaResponse quota = JsonConvert.DeserializeObject<QuotaResponse>(body);

            if (quota != null &&
                quota.type.Equals(LineResponses.QuotaLimited, StringComparison.InvariantCultureIgnoreCase))
                return quota.value;

            return quota is null ? 0 : Int32.MaxValue;
        }

        /// <inheritdoc />
        public async Task<int> GetSentCountThisMonthAsync()
        {
            string body = await GetWithoutQuery(LineApiEndpoints.QuotaConsumptionUri);
            ConsumptionResponse consumption = JsonConvert.DeserializeObject<ConsumptionResponse>(body);

            return consumption?.TotalUsage ?? 0;
        }

        private async Task<string> GetWithoutQuery(Uri uri)
        {
            HttpResponseMessage response = await _httpHandler.GetAsync(uri);
            string body = "";

            if (response?.Content != null)
                body = await response.Content?.ReadAsStringAsync();

            return body;
        }
    }
}