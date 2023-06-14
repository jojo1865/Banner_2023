using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Models;
using Banner.LineBot.Utils.Http;
using Banner.LineBot.Utils.Json;
using Banner.LineBot.Variables;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TextMessage = Banner.LineBot.Models.TextMessage;

namespace Banner.LineBot.Implementation
{
    /// <inheritdoc />
    public class BroadcastLineBot : ILineBot
    {
        private string _channelAccessToken { get; }
        private IHttpHandler _httpClient { get; }

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
        /// <param name="httpClientHandler"><see cref="HttpHandler"/></param>
        public BroadcastLineBot(string channelAccessToken, IHttpHandler httpClientHandler)
        {
            _channelAccessToken = channelAccessToken;
            _httpClient = httpClientHandler;
        }
        
        /// <inheritdoc />
        public async Task<MessagingResult> BroadcastMessageAsync(IMessage message)
        {
            SetHeaders(_httpClient);

            ApiRequest request = MakeRequest(message);
            
            string json = JsonConvert.SerializeObject(request, _serializerSettings);
            
            string response = await SendRequest(_httpClient, json);

            return new MessagingResult
            {
                Success = response == LineResponses.Success,
                Message = response
            };
        }

        private static async Task<string> SendRequest(IHttpHandler httpClient, string requestBodyJson)
        {
            HttpResponseMessage response = await httpClient.PostAsync(LineApiEndpoints.BroadcastMessageUri, requestBodyJson);
            string responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }

        private static ApiRequest MakeRequest(IMessage message)
        {
            ApiRequest request = new ApiRequest();
            request.Messages.Add(message);
            return request;
        }

        private void SetHeaders(IHttpHandler httpClient)
        {
            httpClient.SetFormatAsJson();
            httpClient.SetBearer(_channelAccessToken);
        }
    }
}