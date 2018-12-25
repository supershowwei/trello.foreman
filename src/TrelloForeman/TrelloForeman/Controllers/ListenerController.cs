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

            /*// 不要刪，可以留著 debug 用，儲存 Trello 傳過來的訊息。
            this.SaveResults(rawData);*/

            var triggeredResponse = string.IsNullOrEmpty(rawData) ? null : JSON.DeserializeDynamic(rawData);

            if (triggeredResponse != null)
            {
                var trelloEventHandler = GenerateTrelloEventHandler(triggeredResponse);

                trelloEventHandler.Process(triggeredResponse);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private static ITrelloEventHandler GenerateTrelloEventHandler(dynamic @event)
        {
            var actionType = (string)@event.action.type;
            var listId = (string)@event.model.id;

            if (listId.Equals(TrelloForemanConfig.Instance.BacklogListId)
                && actionType.Equals("createCard", StringComparison.OrdinalIgnoreCase))
            {
                // 在 What's happening 新增卡片
                return new TrelloCreatingCardHandler();
            }

            if (listId.Equals(TrelloForemanConfig.Instance.ToVerifyListId)
                && actionType.Equals("updateCard", StringComparison.OrdinalIgnoreCase)
                && @event.action.data.listAfter != null
                && ((string)@event.action.data.listAfter.id).Equals(TrelloForemanConfig.Instance.ToVerifyListId))
            {
                // 移動卡片到 To Verify
                return new TrelloMovingCardHandler();
            }

            return new NullTrelloEventHandler();
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