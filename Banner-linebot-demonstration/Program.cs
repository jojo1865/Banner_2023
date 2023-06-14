using System;
using System.Configuration;
using System.Threading.Tasks;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Models;
using Banner.LineBot.Models.Messages;

namespace Banner.LineBot.Demo
{
    internal static class Program
    {
        public static async Task Main()
        {
            // 0. 加 bot 好友: @612rsqvr

            // 1. 從 app.config 取得 Line Bot 的 Channel Access Token
            string token = ConfigurationManager.AppSettings["ChannelAccessToken"];

            await GetQuota(token);
            await GetSentCountThisMonth(token);
            await SendMessage(token);
        }

        private static async Task GetQuota(string token)
        {
            IQueryLineBot bot = BotFactory.GetQueryLineBot(token);
            Console.WriteLine("Quota: " + await bot.GetQuota());
        }

        private static async Task GetSentCountThisMonth(string token)
        {
            IQueryLineBot bot = BotFactory.GetQueryLineBot(token);
            Console.WriteLine("Sent this month: " + await bot.GetSentCountThisMonth());
        }

        private static async Task SendMessage(string token)
        {
            // 2. 取得 BroadcastLineBot 實例，並以異步方式進行「主動推播」
            IBroadcastLineBot bot = BotFactory.GetBroadcastLineBot(token);

            int random = new Random().Next();

            MessagingResult result = await bot.BroadcastMessageAsync(new TextMessage
            {
                Text = $"Peko!!! {random}"
            });

            // 3. Success 為 Banner-LineBot 判定結果是否成功，
            //    Message 為 Line Messaging API 回傳 Response
            Console.WriteLine($"result: {result.Success}");
            Console.WriteLine($"message: {result.Message}");
            Console.WriteLine($"random: {random}");
        }
    }
}