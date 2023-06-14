using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Banner.LineBot.Implementation;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Models;
using Banner.LineBot.Utils.Http;
using Moq;
using NUnit.Framework;
using HttpClientHandler = System.Net.Http.HttpClientHandler;

namespace Banner.LineBot.Tests
{
    public class BroadcastLineBotTests
    {
        [Test]
        public async Task BroadcastMessageAsync_WillBroadcastMessage_AndReturnResponseAsString()
        {
            // Arrange
            Uri broadcastMessageUri = new Uri("https://api.line.me/v2/bot/message/broadcast");
            string accessToken =
                "7NWdvcxphKe/68kVpw/B5vMWELLgAD2K6yZUqsftdFEG4xILWq06CQpa5zvclF1PWAvRfi/VMloYycGhh9V7bKGqws+Ywu0ZESZB1ySPIL40DQmJ2jN58wz3LUpjrWcctLPc3D1pjbtJX00w/7JNYwdB04t89/1O/w1cDnyilFU=";

            TextMessage message = new TextMessage
            {
                Text = "Peko!"
            };

            string expectedResponseContent = "{}";
            HttpResponseMessage mockResponse = new HttpResponseMessage
            {
                Content = new StringContent(expectedResponseContent, Encoding.UTF8, "application/json")
            };

            var mockHttpHandler = new Mock<IHttpHandler>();
            mockHttpHandler.Setup(h =>
                    h.PostAsync(broadcastMessageUri, It.Is<string>(c => c.Contains(message.Text))))
                .ReturnsAsync(mockResponse);

            // Act
            ILineBot bot = new BroadcastLineBot(accessToken, mockHttpHandler.Object);
            object response = await bot.BroadcastMessageAsync(message);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsAssignableFrom<MessagingResult>(response);

            MessagingResult result = (MessagingResult)response;
            Assert.IsTrue(result.Success);
            Assert.AreEqual(expectedResponseContent, result.Message);
        }
    }
}