using System.Net.Http;
using System.Threading.Tasks;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Models;
using Banner.LineBot.Models.Messages;
using Banner.LineBot.Utils.Http;
using Banner.LineBot.Utils.Json;
using Banner.LineBot.Variables;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Banner.LineBot.Implementation
{
    /// <inheritdoc />
    internal sealed class BroadcastLineBot : IBroadcastLineBot
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new LowerCaseNamingStrategy()
            }
        };

        /// <summary>
        /// 提供 Line Messaging API 的 Channel Access Token，新建一個 LineBot 實例。
        /// </summary>
        /// <param name="channelAccessToken">Refer to Line Messaging API documentations</param>
        /// <param name="httpHandler">（可選）<see cref="IHttpHandler"/></param>
        public BroadcastLineBot(string channelAccessToken, IHttpHandler httpHandler = null)
        {
            _httpHandler = httpHandler ?? new HttpHandler();
            _httpHandler.SetBearer(channelAccessToken);
        }

        private IHttpHandler _httpHandler { get; }

        /// <inheritdoc />
        public async Task<MessagingResult> BroadcastMessageAsync(IMessage message)
        {
            MessagingApiRequest request = new MessagingApiRequest(message);
            string response = await PostRequest(request);

            return new MessagingResult
            {
                Success = response == LineResponses.Success,
                Message = response
            };
        }

        private async Task<string> PostRequest(MessagingApiRequest request)
        {
            string json = JsonConvert.SerializeObject(request, _serializerSettings);
            HttpResponseMessage response = await _httpHandler.PostAsync(LineApiEndpoints.BroadcastMessageUri, json);

            return await response.Content.ReadAsStringAsync();
        }
    }
}