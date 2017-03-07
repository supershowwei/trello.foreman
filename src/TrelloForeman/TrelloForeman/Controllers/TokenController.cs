using System.Web.Mvc;

namespace TrelloForeman.Controllers
{
    [RoutePrefix("token")]
    public class TokenController : Controller
    {
        [Route]
        public ActionResult Index()
        {
            return this.View();
        }
    }
}