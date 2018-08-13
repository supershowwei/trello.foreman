using System;
using System.IO;
using System.Net;
using System.Web.Mvc;
using Jil;
using TrelloForeman.Contract;
using TrelloForeman.Logic;

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

            if (triggeredResponse != null)
            {
                var trelloEventHandler = GenerateTrelloEventHandler(triggeredResponse);

                trelloEventHandler.Process(triggeredResponse);
            }

            /*// 不要刪，可以留著 debug 用，儲存 Trello 傳過來的訊息。
            this.SaveResults(rawData);
            */
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private static ITrelloEventHandler GenerateTrelloEventHandler(dynamic @event)
        {
            var actionType = (string)@event.action.type;
            var listId = (string)@event.model.id;

            if (IsBacklogAdded(listId, actionType))
            {
                // 在 What's happening 新增卡片
                return new TrelloCardAddedHandler();
            }

            if (IsBacklogNeedVerify(listId, actionType, @event.action.data.listAfter))
            {
                // 移動卡片到 To Verify
                return new TrelloCardMovedHandler();
            }

            if (IsBacklogDone(listId, actionType, @event.action.data.listAfter))
            {
                return new TrelloCardDoneHandler();
            }

            return new NullTrelloEventHandler();
        }

        private static bool IsBacklogAdded(string listId, string actionType)
        {
            if (string.IsNullOrEmpty(TrelloForemanConfig.Instance.BacklogListId)) return false;
            if (!listId.Equals(TrelloForemanConfig.Instance.BacklogListId)) return false;
            if (!actionType.Equals("createCard", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        private static bool IsBacklogNeedVerify(string listId, string actionType, dynamic listAfter)
        {
            if (string.IsNullOrEmpty(TrelloForemanConfig.Instance.ToVerifyListId)) return false;
            if (!listId.Equals(TrelloForemanConfig.Instance.ToVerifyListId)) return false;
            if (!actionType.Equals("updateCard", StringComparison.OrdinalIgnoreCase)) return false;
            if (listAfter == null) return false;
            if (!((string)listAfter.id).Equals(TrelloForemanConfig.Instance.ToVerifyListId)) return false;

            return true;
        }

        private static bool IsBacklogDone(string listId, string actionType, dynamic listAfter)
        {
            if (string.IsNullOrEmpty(TrelloForemanConfig.Instance.DoneListId)) return false;
            if (!listId.Equals(TrelloForemanConfig.Instance.DoneListId)) return false;
            if (!actionType.Equals("updateCard", StringComparison.OrdinalIgnoreCase)) return false;
            if (listAfter == null) return false;
            if (!((string)listAfter.id).Equals(TrelloForemanConfig.Instance.DoneListId)) return false;

            return true;
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