﻿using System.Linq;
using System.Net;
using System.Web.Mvc;
using Manatee.Trello;

namespace TrelloForeman.Controllers
{
    public class DeveloperController : Controller
    {
        [Route("~/card/{cardId}/assign/{memberId}")]
        public ActionResult Assign(string cardId, string memberId)
        {
            var card = new Card(cardId, TrelloAuthorization.Default);

            card.Members.Add(new Member(memberId, TrelloAuthorization.Default));

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [Route("~/card/{cardId}/move/{listId}")]
        public ActionResult MoveCard(string cardId, string listId)
        {
            var card = new Card(cardId, TrelloAuthorization.Default);
            var destinationList = new List(listId, TrelloAuthorization.Default);

            card.List = destinationList;
            card.Position = Position.Top;

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [Route("~/list/{listId}/webhook/create")]
        public ActionResult CreateWebhook(string listId)
        {
            var list = new List(listId);

            var webhook = new Webhook<List>(
                list,
                TrelloForemanConfig.Instance.ListenerUrl,
                auth: TrelloAuthorization.Default);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [Route("~/card/{cardId}")]
        public ActionResult Card(string cardId)
        {
            var card = new Card(cardId, TrelloAuthorization.Default);

            this.ViewBag.Card = new
                                {
                                    card.Id,
                                    card.Name,
                                    card.ShortUrl,
                                    card.DueDate,
                                    card.Members
                                };

            return this.View();
        }

        [Route("~/list/{listId}")]
        public ActionResult List(string listId)
        {
            var list = new List(listId, TrelloAuthorization.Default);

            this.ViewBag.List = new { list.Name, list.IsArchived, Board = new { list.Board.Id, list.Board.Name } };

            return this.View();
        }

        [Route("~/list/{listId}/actions")]
        public ActionResult ActionsOfList(string listId)
        {
            var list = new List(listId, TrelloAuthorization.Default);

            this.ViewBag.Actions = list.Actions.Take(5)
                .Select(x => new { x.Id, x.Type, Creator = new { x.Creator.Id, x.Creator.FullName }, x.CreationDate });

            return this.View();
        }

        [Route("~/list/{listId}/cards")]
        public ActionResult CardsOfList(string listId)
        {
            var list = new List(listId, TrelloAuthorization.Default);

            this.ViewBag.Cards = list.Cards.Select(
                c => new
                     {
                         c.Id,
                         c.Name,
                         c.ShortUrl,
                         c.DueDate,
                         Members = c.Members.Select(m => new { m.Id, m.FullName }),
                         Actions = c.Actions.Filter(ActionType.CreateCard)
                             .Select(a => new { a.Id, a.Type, a.Creator.FullName })
                     });

            return this.View();
        }

        [Route("~/board/{boardId}/lists")]
        public ActionResult ListsOfBoard(string boardId)
        {
            var board = new Board(boardId, TrelloAuthorization.Default);

            this.ViewBag.Lists = board.Lists.Select(x => new { x.Id, x.Name });

            return this.View();
        }

        [Route("~/board/{boardId}/members")]
        public ActionResult MembersOfBoard(string boardId)
        {
            var board = new Board(boardId, TrelloAuthorization.Default);

            this.ViewBag.Members = board.Members.Select(x => new { x.Id, x.FullName });

            return this.View();
        }

        [Route("~/card/{cardId}/members")]
        public ActionResult MembersOfCard(string cardId)
        {
            var card = new Card(cardId, TrelloAuthorization.Default);

            this.ViewBag.Members = card.Members.Select(m => new { m.Id, m.FullName });

            return this.View();
        }

        [Route("~/member/{memberId}/boards")]
        public ActionResult BoardsOfMember(string memberId)
        {
            var member = new Member(memberId, TrelloAuthorization.Default);

            this.ViewBag.Boards = member.Boards.Select(x => new { x.Id, x.Name });

            return this.View();
        }
    }
}