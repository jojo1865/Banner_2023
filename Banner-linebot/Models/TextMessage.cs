using System;
using Newtonsoft.Json;

namespace Banner.LineBot.Models
{
    /// <summary>
    /// 用於 Line Messaging API 的文字訊息物件。
    /// </summary>
    public sealed class TextMessage : IMessage
    {
        [JsonProperty("type")]
        public string Type => "text";
        
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}