using Banner.LineBot.Implementation;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Utils.Http;

namespace Banner.LineBot
{
    /// <summary>
    /// 用於取得 Line Bot 物件的靜態工具。
    /// </summary>
    public static class BotFactory
    {
        /// <summary>
        /// 取得推播訊息用的 Line Bot 物件。
        /// </summary>
        /// <param name="token">Channel Access Token</param>
        /// <param name="handler">（可選）<see cref="IHttpHandler"/></param>
        /// <returns>推播訊息用的 LineBot</returns>
        public static IBroadcastLineBot GetBroadcastLineBot(string token, IHttpHandler handler = null)
        {
            return new BroadcastLineBot(token, handler);
        }

        /// <summary>
        /// 取得查詢 API 狀態用的 Line Bot 物件。
        /// </summary>
        /// <param name="token">Channel Access Token</param>
        /// <param name="handler">（可選）<see cref="IHttpHandler"/></param>
        /// <returns>查詢狀態用的 LineBot</returns>
        public static IQueryLineBot GetQueryLineBot(string token, IHttpHandler handler = null)
        {
            return new QueryLineBot(token, handler);
        }
    }
}