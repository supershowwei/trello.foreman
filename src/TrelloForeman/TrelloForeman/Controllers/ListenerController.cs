using System;
using System.IO;
using System.Net;
using System.Web.Mvc;
using Jil;
using Manatee.Trello;
using RestSharp;

namespace TrelloForeman.Controllers
{
    [RoutePrefix("listener")]
    public class ListenerController : Controller
    {
        [Route("listen")]
        public ActionResult Listen()
        {
            var rawData = this.GetRawData();

            var triggeredResponse = string.IsNullOrEmpty(rawData) ? null : JSON.DeserializeDynamic(rawData);

            if ((triggeredResponse != null)
                && ((string)triggeredResponse.action.type).Equals("createCard", StringComparison.OrdinalIgnoreCase))
            {
                // 指定一個人去處理
                var card = new Card((string)triggeredResponse.action.data.card.id, TrelloAuthorization.Default);
                var member = new Member(TrelloForemanConfig.Instance.FetchOneWorker(), TrelloAuthorization.Default);

                card.Members.Add(member);

                // 移動到指定的泳道
                var todoList = new List(TrelloForemanConfig.Instance.ToDoListId, TrelloAuthorization.Default);

                card.List = todoList;
                card.Position = Position.Top;

                // 發釘釘通知
                this.Notify(member, card);
            }

            /* 不要刪，可以留著 debug 用，儲存 Trello 傳過來的訊息。
            this.SaveResults(rawData);
            */
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private void Notify(Member member, Card card)
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
                                new
                                    {
                                        content = $"[你强] {card.Name}\r\n\r\n指定 '{member.FullName}' 處理\r\n\r\n{card.ShortUrl}"
                                    }
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

        private string GetRawData()
        {
            try
            {
                this.Request.InputStream.Seek(0, SeekOrigin.Begin);

                using (var sr = new StreamReader(this.Request.InputStream))
                {
                    return sr.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }

        private void SaveResults(string rawData)
        {
            System.IO.File.WriteAllText(
                Path.Combine(this.Server.MapPath("~/App_Data"), DateTime.Now.ToString("yyyyMMddHHmmssfff")),
                rawData);
        }
    }
}