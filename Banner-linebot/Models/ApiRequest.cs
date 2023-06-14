using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Banner.LineBot.Models
{
    internal class ApiRequest
    {
        [JsonProperty("messages")]
        public ICollection<IMessage> Messages { get; } = new List<IMessage>();

        public ApiRequest(IMessage message)
        {
            Messages.Add(message);
        }

        public ApiRequest(IEnumerable<IMessage> messages)
        {
            foreach (IMessage message in messages)
            {
                Messages.Add(message);
            }
        }
    }
}