using System.Web.Mvc;

namespace TrelloForeman.Controllers
{
    [RoutePrefix("token")]
    public class TokenController : Controller
    {
        // GET: Token
        [Route]
        public ActionResult Index()
        {
            return this.View();
        }
    }
}