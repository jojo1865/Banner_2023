using System.Collections.Generic;
using Newtonsoft.Json;

namespace Banner.LineBot.Models
{
    internal class ApiRequest
    {
        [JsonProperty("messages")]
        public ICollection<IMessage> Messages { get; } = new List<IMessage>();
    }
}