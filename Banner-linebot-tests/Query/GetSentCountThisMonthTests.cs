using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Banner.LineBot.Interfaces;
using Banner.LineBot.Utils.Http;
using Moq;
using NUnit.Framework;

namespace Banner.LineBot.Tests.Query
{
    public class GetSentCountThisMonthTests
    {
        [Test]
        public async Task GetSentCountThisMonthTests_WillCheckQuota_AndReturnInt()
        {
            // Arrange
            Uri consumptionUri = new Uri("https://api.line.me/v2/bot/message/quota/consumption");
            string accessToken = "test";

            int expectedInt = new Random().Next();

            HttpResponseMessage mockResponse = Arrange_MockResponse(expectedInt);

            Mock<IHttpHandler> mockHttpHandler = Arrange_MockHttpHandler(consumptionUri, mockResponse);

            // Act
            IQueryLineBot bot = BotFactory.GetQueryLineBot(accessToken, mockHttpHandler.Object);
            object response = await bot.GetSentCountThisMonthAsync();

            // Assert
            Assert.IsNotNull(response);
            Assert.IsAssignableFrom<int>(response);

            int result = (int)response;
            Assert.AreEqual(expectedInt, result);
        }

        private static HttpResponseMessage Arrange_MockResponse(int testValue)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent($"{{\"totalUsage\":\"{testValue}\"}}", Encoding.UTF8, "application/json")
            };
        }

        private static Mock<IHttpHandler> Arrange_MockHttpHandler(Uri uri,
            HttpResponseMessage mockResponse)
        {
            Mock<IHttpHandler> mockHttpHandler = new Mock<IHttpHandler>();
            mockHttpHandler.Setup(h =>
                    h.GetAsync(uri))
                .ReturnsAsync(mockResponse);
            return mockHttpHandler;
        }
    }
}