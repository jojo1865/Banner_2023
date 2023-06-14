using Newtonsoft.Json.Serialization;

namespace Banner.LineBot.Utils.Json
{
    public class LowerCaseNamingStrategy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            return name.ToLowerInvariant();
        }
    }
}