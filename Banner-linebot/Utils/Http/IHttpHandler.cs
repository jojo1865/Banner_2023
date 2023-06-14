using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Banner.LineBot.Utils.Http
{
    /// <summary>
    /// 處理 Http Requesting 的抽象物件。
    /// </summary>
    public interface IHttpHandler
    {
        /// <summary>
        /// 異步地 POST 一個要求。
        /// </summary>
        /// <param name="url">對象 URI</param>
        /// <param name="requestBodyJson">Request Body JSON</param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        Task<HttpResponseMessage> PostAsync(Uri url, string requestBodyJson);

        /// <summary>
        /// 異步地 GET。
        /// </summary>
        /// <param name="url">對象 URI</param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        Task<HttpResponseMessage> GetAsync(Uri url);

        /// <summary>
        /// 異步地 GET。
        /// </summary>
        /// <param name="url">對象 URI</param>
        /// <param name="query">用於組成查詢字串的物件</param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        Task<HttpResponseMessage> GetAsync<T>(Uri url, T query);

        /// <summary>
        /// 設定 Bearer。
        /// </summary>
        /// <param name="token">Token</param>
        void SetBearer(string token);
    }
}