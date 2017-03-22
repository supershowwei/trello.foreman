using System;
using System.Collections.Generic;
using System.Linq;
using Jil;
using Manatee.Trello;
using RestSharp;
using TrelloForeman.Contract;
using TrelloForeman.Models;
using Member = TrelloForeman.Models.Member;

namespace TrelloForeman.Logic
{
    public class TrelloCreatingCardHandler : ITrelloEventHandler
    {
        public void Process(dynamic @event)
        {
            // 檢查卡片是否已經有 Member
            var card = new Card((string)@event.action.data.card.id, TrelloAuthorization.Default);

            if (card.Members.Count() == 0)
            {
                // 指定一個非休假人員去處理
                var worker = GetOneNonLeaveWorker();
                var trelloMember = new Manatee.Trello.Member(worker.Id, TrelloAuthorization.Default);

                card.Members.Add(trelloMember);

                // 發釘釘通知
                Notify(trelloMember, worker.CellphoneNumber, card, (string)@event.action.memberCreator.fullName);
            }
        }

        private static Member GetOneNonLeaveWorker()
        {
            Member worker;
            do
            {
                worker = TrelloForemanConfig.Instance.FetchOneWorker();
            }
            while (IsLeaveWorker(worker.Id));

            return worker;
        }

        private static bool IsLeaveWorker(string workerId)
        {
            var leaveMembers = GetLeaveMembers();

            return
                leaveMembers.Any(
                    m =>
                        m.Id.Equals(workerId, StringComparison.OrdinalIgnoreCase) && m.DueDate.Date == DateTime.Now.Date
                        && DateTime.Now < m.DueDate);
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

        private static void Notify(
            Manatee.Trello.Member trelloMember,
            string cellphoneNumber,
            Card card,
            string creatorFullName)
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
                                        $"[你强] {creatorFullName} 回報\r\n{card.Name}\r\n\r\n指定給 {trelloMember.FullName} 處理\r\n卡片連結：{card.ShortUrl}"
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
    }
}