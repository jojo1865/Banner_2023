using System.Threading.Tasks;

namespace Banner.LineBot.Interfaces
{
    /// <summary>
    /// 與 LINE Messaging API 的溝通介面，主要負責查詢 Bot 相關資訊。
    /// </summary>
    public interface IQueryLineBot
    {
        /// <summary>
        /// 取得目前方案的訊息額度（訊息數）。
        /// </summary>
        /// <returns>
        /// API 有做額度限制時：目前方案的訊息額度（訊息數）<br/>
        /// 沒有額度限制時：固定回傳 int 最大值
        /// </returns>
        /// <remarks>訊息數的計算方式，請參考 LINE Messaging API 的官方文件。</remarks>
        Task<int> GetQuotaAsync();

        /// <summary>
        /// 取得這個月已經發送了多少訊息。
        /// </summary>
        /// <returns>本月已發送訊息數</returns>
        /// <remarks>訊息數的計算方式，請參考 LINE Messaging API 的官方文件。</remarks>
        Task<int> GetSentCountThisMonthAsync();
    }
}