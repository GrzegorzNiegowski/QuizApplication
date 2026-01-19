using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Models.ViewModels;
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
            if (dto == null)
            {
                ViewBag.Error = "Brak danych.";
                return View();
            }

            var code = (dto.SessionCode ?? "").Trim().ToUpperInvariant();
            var nick = (dto.PlayerName ?? "").Trim();
            // Tu tylko wstępna walidacja, właściwe dołączenie przez SignalR w JS
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nick))
            {
                ViewBag.Error = "Dane są wymagane";
                return View();
            }
            if (!_gameSessionService.SessionExists(code))
            {
                ViewBag.Error = "Sesja nie istnieje";
                return View();
            }

            
            return RedirectToAction(nameof(Lobby), new {code, nick});
        }


        // GET: /Game/Lobby (Widok oczekiwania dla Gracza)
        public IActionResult Lobby(string code, string nick)
        {
            var normCode = (code ?? "").Trim().ToUpperInvariant();
            var normNick = (nick ?? "").Trim();

            if (!_gameSessionService.SessionExists(normCode))
            {
                return RedirectToAction("Join");
            }
            if (string.IsNullOrWhiteSpace(normCode) || string.IsNullOrWhiteSpace(normNick))
                return RedirectToAction(nameof(Join));

            if (!_gameSessionService.SessionExists(normCode))
                return RedirectToAction(nameof(Join));

            ViewBag.Code = normCode;
            ViewBag.Nick = normNick;
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
            gameQuizDto.AccessCode = (gameQuizDto.AccessCode ?? "").Trim().ToUpperInvariant();
            // Inicjujemy sesję w pamięci
            _gameSessionService.InitializeSession(new StartSessionDto { QuizId = id }, gameQuizDto);

            return RedirectToAction("LobbyHost", new { code = gameQuizDto.AccessCode });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult LobbyHost(string code)
        {
            var normCode = (code ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normCode))
                return RedirectToAction(nameof(Join));

            if (!_gameSessionService.SessionExists(normCode))
                return RedirectToAction(nameof(Join));

            ViewBag.AccessCode = normCode;
            return View();
        }

        
        public IActionResult Play(string code, string nick)
        {
            var normCode = (code ?? "").Trim().ToUpperInvariant();
            var normNick = (nick ?? "").Trim();

            if (string.IsNullOrWhiteSpace(normCode) || string.IsNullOrWhiteSpace(normNick))
                return RedirectToAction(nameof(Join));

            if (!_gameSessionService.SessionExists(normCode))
                return RedirectToAction(nameof(Join));

            return View(new PlayViewModel { AccessCode = normCode, Nickname = normNick });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]

        public IActionResult PlayHost(string code)
        {
            var normCode = (code ?? "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(normCode))
                return RedirectToAction(nameof(Join));

            if (!_gameSessionService.SessionExists(normCode))
                return RedirectToAction(nameof(Join));

            return View(new PlayHostViewModel { AccessCode = normCode });
        }

    }
}
