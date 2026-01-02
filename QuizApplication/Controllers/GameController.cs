using Microsoft.AspNetCore.Mvc;

namespace QuizApplication.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Join()
        {
            return View();
        }

        public IActionResult Lobby(string code, string nick)
        {
            ViewBag.Code = code;
            ViewBag.Nick = nick;
            return View();
        }
    }
}
