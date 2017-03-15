using Jil;
using Manatee.Trello;
using RestSharp;
using TrelloForeman.Contract;

namespace TrelloForeman.Logic
{
    public class TrelloMovingCardHandler : ITrelloEventHandler
    {
        public void Process(dynamic @event)
        {
            var card = new Card((string)@event.action.data.card.id, TrelloAuthorization.Default);
            var memberCellphoneNumber =
                TrelloForemanConfig.Instance.FetchMemberCellphoneNumber((string)@event.action.memberCreator.id);

            // 發釘釘通知
            Notify((string)@event.action.memberCreator.fullName, memberCellphoneNumber, card);
        }

        private static void Notify(string memberName, string cellphoneNumber, Card card)
        {
            try
            {
                var client = new RestClient(TrelloForemanConfig.Instance.DingtalkWebhookUrl);

                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");

                request.AddParameter(
                    "application/json",
                    JSON.Serialize(
                        new
                            {
                                msgtype = "text",
                                text =
                                new { content = $"[跳舞] 請 {memberName} 檢查\r\n{card.Name}\r\n\r\n卡片連結：{card.ShortUrl}" },
                                at = new { atMobiles = new[] { cellphoneNumber }, isAtAll = false }
                            },
                        new Options(
                            dateFormat: DateTimeFormat.ISO8601,
                            serializationNameFormat: SerializationNameFormat.CamelCase)),
                    ParameterType.RequestBody);

                client.Execute(request);
            }
            catch
            {
                // 失敗了就失敗了，目前暫不做處理。
            }
        }
    }
}