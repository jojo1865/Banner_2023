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

            string expectedResponseContent = "{}";
            
            TextMessage message = Arrange_MockTextMessage();
            
            HttpResponseMessage mockResponse = Arrange_MockResponse(expectedResponseContent);

            Mock<IHttpHandler> mockHttpHandler = Arrange_MockHttpHandler(broadcastMessageUri, message, mockResponse);

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

        private static TextMessage Arrange_MockTextMessage()
        {
            return new TextMessage
            {
                Text = "Peko!"
            };
        }

        private static HttpResponseMessage Arrange_MockResponse(string expectedResponseContent)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(expectedResponseContent, Encoding.UTF8, "application/json")
            };
        }

        private static Mock<IHttpHandler> Arrange_MockHttpHandler(Uri broadcastMessageUri, TextMessage message,
            HttpResponseMessage mockResponse)
        {
            Mock<IHttpHandler> mockHttpHandler = new Mock<IHttpHandler>();
            mockHttpHandler.Setup(h =>
                    h.PostAsync(broadcastMessageUri, It.Is<string>(c => c.Contains(message.Text))))
                .ReturnsAsync(mockResponse);
            return mockHttpHandler;
        }
    }
}