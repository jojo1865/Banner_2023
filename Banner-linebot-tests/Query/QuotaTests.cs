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
    public class QuotaTests
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public async Task GetQuotaAsync_WillCheckQuota_AndReturnInt(bool isLimited)
        {
            // Arrange
            Uri quotaUri = new Uri("https://api.line.me/v2/bot/message/quota");
            string accessToken = "test";

            int expectedInt = new Random().Next();

            HttpResponseMessage mockResponse = Arrange_MockResponse(expectedInt, isLimited);

            Mock<IHttpHandler> mockHttpHandler = Arrange_MockHttpHandler(quotaUri, mockResponse);

            // Act
            IQueryLineBot bot = BotFactory.GetQueryLineBot(accessToken, mockHttpHandler.Object);
            object response = await bot.GetQuotaAsync();

            // Assert
            Assert.IsNotNull(response);
            Assert.IsAssignableFrom<int>(response);

            int result = (int)response;
            // 當 API 回傳中的 type 不是 Limited 時，則應回傳 int 最大值
            Assert.AreEqual(isLimited ? expectedInt : Int32.MaxValue, result);
        }

        [Test]
        public async Task GetQuotaAsync_WillCheckResponseIsNull_ThenReturnZero()
        {
            // Arrange
            Uri quotaUri = new Uri("https://this.is.a.wrong.uri/");
            string accessToken = "test";

            int expectedInt = new Random().Next();

            HttpResponseMessage mockResponse = Arrange_MockResponse(expectedInt);

            Mock<IHttpHandler> mockHttpHandler = Arrange_MockHttpHandler(quotaUri, mockResponse);

            // Act
            IQueryLineBot bot = BotFactory.GetQueryLineBot(accessToken, mockHttpHandler.Object);
            object response = await bot.GetQuotaAsync();

            // Assert
            Assert.IsNotNull(response);
            Assert.IsAssignableFrom<int>(response);

            int result = (int)response;
            Assert.AreEqual(0, result);
        }

        private static HttpResponseMessage Arrange_MockResponse(int testValue, bool isLimited = false)
        {
            string type = isLimited ? "limited" : "none";
            return new HttpResponseMessage
            {
                Content = new StringContent($"{{\"type\":\"{type}\", \"value\":\"{testValue}\"}}", Encoding.UTF8,
                    "application/json")
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