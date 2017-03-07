using System.IO;
using System.Web;
using System.Xml.Linq;
using Manatee.Trello;
using Manatee.Trello.ManateeJson;
using Manatee.Trello.RestSharp;

namespace TrelloForeman
{
    public class ManateeTrelloConfig
    {
        public static void Configure()
        {
            var serializer = new ManateeSerializer();
            TrelloConfiguration.Serializer = serializer;
            TrelloConfiguration.Deserializer = serializer;
            TrelloConfiguration.JsonFactory = new ManateeFactory();
            TrelloConfiguration.RestClientProvider = new RestSharpClientProvider();
            TrelloAuthorization.Default.AppKey = TrelloForemanConfig.Instance.ApplicationKey;
            TrelloAuthorization.Default.UserToken = TrelloForemanConfig.Instance.UserToken;
        }
    }
}