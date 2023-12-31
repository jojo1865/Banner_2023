﻿using System.Threading.Tasks;
using Banner.LineBot.Models;
using Banner.LineBot.Models.Messages;

namespace Banner.LineBot.Interfaces
{
    /// <summary>
    /// 與 LINE Messaging API 的溝通介面，主要負責推播功能。
    /// </summary>
    public interface IBroadcastLineBot
    {
        /// <summary>
        /// 異步地向所有好友推播訊息。
        /// </summary>
        /// <param name="message">訊息物件</param>
        /// <returns><see cref="MessagingResult"/></returns>
        Task<MessagingResult> BroadcastMessageAsync(IMessage message);
    }
}