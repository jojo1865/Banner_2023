namespace Banner.LineBot.Models.Messages
{
    /// <summary>
    /// 用於 Line Messaging API 的文字訊息物件。
    /// </summary>
    public sealed class TextMessage : IMessage
    {
        public string Type => "text";
        public string Text { get; set; }
    }
}