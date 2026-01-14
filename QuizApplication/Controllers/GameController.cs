using Microsoft.AspNetCore.Mvc;
using QuizApplication.Services;

namespace QuizApplication.Controllers
{
    public class GameController : Controller
    {
        private readonly IGameSessionService _gameSessionService;
        private readonly IQuizService _quizService; // Potrzebny, by pobrać kod quizu dla Hosta

        public GameController(IGameSessionService gameSessionService, IQuizService quizService)
        {
            _gameSessionService = gameSessionService;
            _quizService = quizService;
        }


        // --- Gracz ---
        public IActionResult Join()
        {
            return View();
        }

        // POST: /Game/Join (Walidacja formularza przed wejściem do Lobby)
        [HttpPost]
        public IActionResult Join(string accesCode, string nickname)
        {
            if(string.IsNullOrWhiteSpace(accesCode) || string.IsNullOrWhiteSpace(nickname))
            {
                ViewBag.Error = "Kod i nick są wymagane";
                return View();
            }
            // Sprawdź czy sesja w ogóle istnieje w pamięci
            if(!_gameSessionService.SessionExists(accesCode))
            {
                ViewBag.Error = "Nie znaleziono aktywnej gry o takim kodzie";
                return View();
            }

            if (_gameSessionService.IsNicknameTaken(accesCode, nickname))
            {
                ViewBag.Error = "Ten nick jest już zajęty w tej grze.";
                // Zwracamy widok Join, użytkownik nie przechodzi dalej
                return View();
            }

            ViewBag.Code = accesCode;
            ViewBag.Nick = nickname;
            return View();
        }


        // GET: /Game/Lobby (Widok oczekiwania dla Gracza)
        public IActionResult Lobby(string code, string nick)
        {
            if(!_gameSessionService.SessionExists(code))
            {
                return RedirectToAction("Join");
            }
            ViewBag.Code = code;
            ViewBag.Nick = nick;
            return View();
        }


        // --- DLA HOSTA ---
        // Akcja wywoływana z przycisku "Zagraj/Hostuj" w szczegółach quizu
        // GET: /Game/HostGame/5
        [Microsoft.AspNetCore.Authorization.Authorize] // Tylko zalogowany
        public async Task<IActionResult> HostGame(int id)
        {
            // Teraz pobieramy DTO
            var result = await _quizService.GetQuizByIdAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var quizDto = result.Data;

            _gameSessionService.InitializeSession(quizDto.AccessCode, quizDto.Id);

            return RedirectToAction("LobbyHost", new { code = quizDto.AccessCode });
        }

        // GET: /Game/LobbyHost (Panel sterowania Hosta)
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult LobbyHost(string code)
        {
            if(!_gameSessionService.SessionExists(code))
            {
                return RedirectToAction("Index", "Quizzes"); //wracamy do listy jeśli sesja wygasła
            }
            ViewBag.Code = code;
            // Tutaj nie potrzebujemy nicku, bo to Host
            return View();
        }
    }
}
