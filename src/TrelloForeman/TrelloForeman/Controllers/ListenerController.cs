namespace TrelloForeman.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web.Mvc;

    using Jil;

    using Manatee.Trello;

    using RestSharp;

    using TrelloForeman.Models;

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
                // 指定一個非休假人員去處理
                var worker = GetOneNonLeaveWorker();
                var card = new Card((string)triggeredResponse.action.data.card.id, TrelloAuthorization.Default);
                var member = new Member(worker.Id, TrelloAuthorization.Default);

                card.Members.Add(member);

                // 發釘釘通知
                Notify(member, worker.CellphoneNumber, card, (string)triggeredResponse.action.memberCreator.fullName);
            }

            /* 不要刪，可以留著 debug 用，儲存 Trello 傳過來的訊息。
            this.SaveResults(rawData);
            */
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private static List<LeaveMember> GetLeaveMembers()
        {
            var vacationList = new List(TrelloForemanConfig.Instance.VacationListId, TrelloAuthorization.Default);

            var leaveMembers = new List<LeaveMember>();

            vacationList.Cards.ToList()
                .ForEach(
                    card => { leaveMembers.AddRange(card.Members.Select(m => new LeaveMember(m.Id, card.DueDate))); });

            return leaveMembers;
        }

        private static Worker GetOneNonLeaveWorker()
        {
            Worker member;
            do
            {
                member = TrelloForemanConfig.Instance.FetchOneWorker();
            }
            while (IsLeaveWorker(member.Id));

            return member;
        }

        private static bool IsLeaveWorker(string workerId)
        {
            var leaveMembers = GetLeaveMembers();

            return
                leaveMembers.Any(
                    m =>
                        m.Id.Equals(workerId, StringComparison.OrdinalIgnoreCase)
                        && (m.DueDate.Date == DateTime.Now.Date) && (DateTime.Now < m.DueDate));
        }

        private static void Notify(Member member, string cellphoneNumber, Card card, string creatorFullName)
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
                                        content =
                                        $"[你强] {creatorFullName} 回報\r\n{card.Name}\r\n\r\n指定給 {member.FullName} 處理\r\n卡片連結：{card.ShortUrl}"
                                    },
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