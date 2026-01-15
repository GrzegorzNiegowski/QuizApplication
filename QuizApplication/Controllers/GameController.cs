using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.SessionDtos;
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
        public IActionResult Join(JoinSessionDto dto)
        {
            // Tu tylko wstępna walidacja, właściwe dołączenie przez SignalR w JS
            if (string.IsNullOrWhiteSpace(dto.SessionCode) || string.IsNullOrWhiteSpace(dto.PlayerName))
            {
                ViewBag.Error = "Dane są wymagane";
                return View();
            }
            if (!_gameSessionService.SessionExists(dto.SessionCode))
            {
                ViewBag.Error = "Sesja nie istnieje";
                return View();
            }

            ViewBag.Code = dto.SessionCode;
            ViewBag.Nick = dto.PlayerName;
            return View("Lobby");
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
            // POBIERAMY DANE DO GRY (FULL)
            var result = await _quizService.GetQuizForGameAsync(id);
            if (!result.Success || result.Data == null) return NotFound();

            var gameQuizDto = result.Data;

            // Inicjujemy sesję w pamięci
            _gameSessionService.InitializeSession(new StartSessionDto { QuizId = id }, gameQuizDto);

            return RedirectToAction("LobbyHost", new { code = gameQuizDto.AccessCode });
        }

        // GET: /Game/LobbyHost (Panel sterowania Hosta)
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult LobbyHost(string code)
        {
            if(!_gameSessionService.SessionExists(code))
            {
                return RedirectToAction("Index", "Quizzes"); //wracamy do listy jeśli sesja wygasła
            }
            ViewBag.AccessCode = code;
            // Tutaj nie potrzebujemy nicku, bo to Host
            return View();
        }

        // --- ROZGRYWKA (NOWA AKCJA) ---
        // Tutaj trafią wszyscy po sygnale "GameStarted"
        public IActionResult Play(string code, string? nick)
        {
            if(!_gameSessionService.SessionExists(code))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Code = code;

            // Sprawdzamy czy to host (zalogowany właściciel) czy gracz (ma nick)
            // Uproszczenie: Jeśli nick jest pusty, zakładamy że to Host (zalogowany w systemie)
            bool isHost = string.IsNullOrEmpty(nick);
            ViewBag.IsHost = isHost;
            ViewBag.Nick = nick ?? "Host";
            return View();
        }
    }
}
