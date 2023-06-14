using System.Collections.Generic;

namespace Banner.LineBot.Models.Messages
{
    internal class MessagingApiRequest
    {
        public MessagingApiRequest(IMessage message)
        {
            Messages.Add(message);
        }

        public MessagingApiRequest(IEnumerable<IMessage> messages)
        {
            foreach (IMessage message in messages)
            {
                Messages.Add(message);
            }
        }

        public ICollection<IMessage> Messages { get; } = new List<IMessage>();
    }
}