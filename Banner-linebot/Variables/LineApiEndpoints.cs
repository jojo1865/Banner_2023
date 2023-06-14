using System;

namespace Banner.LineBot.Variables
{
    internal static class LineApiEndpoints
    {
        public static readonly Uri BroadcastMessageUri = new Uri("https://api.line.me/v2/bot/message/broadcast");
    }
}