using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Banner.LineBot.Utils.Http
{
    public interface IHttpHandler
    {
        Task<HttpResponseMessage> PostAsync(Uri url, string requestBodyJson);
        void SetFormatAsJson();
        void SetBearer(string token);
    }
}