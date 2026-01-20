using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Models.ViewModels;
using QuizApplication.Services;
using QuizApplication.Utilities;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    public class GameController : Controller
    {
        private readonly IGameSessionService _gameSessionService;
        private readonly IQuizService _quizService;
        private readonly ILogger<GameController> _logger; // Potrzebny, by pobrać kod quizu dla Hosta

        public GameController(
        IGameSessionService gameSessionService,
        IQuizService quizService,
        ILogger<GameController> logger)
        {
            _gameSessionService = gameSessionService;
            _quizService = quizService;
            _logger = logger;
        }

        #region Player Actions

        // GET: /Game/Join
        public IActionResult Join()
        {
            return View();
        }

        // POST: /Game/Join
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Join(JoinSessionDto dto)
        {
            if (dto == null)
            {
                ViewBag.Error = "Brak danych.";
                return View();
            }

            var code = SessionCodeHelper.Normalize(dto.SessionCode);
            var nick = (dto.PlayerName ?? "").Trim();

            if (!SessionCodeHelper.IsValid(code) || string.IsNullOrWhiteSpace(nick))
            {
                ViewBag.Error = "Dane są wymagane i muszą być poprawne";
                return View();
            }

            if (!_gameSessionService.SessionExists(code))
            {
                ViewBag.Error = "Sesja nie istnieje";
                return View();
            }

            return RedirectToAction(nameof(Lobby), new { code, nick });
        }

        // GET: /Game/Lobby
        public IActionResult Lobby(string code, string nick)
        {
            var normCode = SessionCodeHelper.Normalize(code);
            var normNick = (nick ?? "").Trim();

            if (!SessionCodeHelper.IsValid(normCode) || string.IsNullOrWhiteSpace(normNick))
            {
                return RedirectToAction(nameof(Join));
            }

            if (!_gameSessionService.SessionExists(normCode))
            {
                TempData["Error"] = "Sesja nie istnieje lub wygasła";
                return RedirectToAction(nameof(Join));
            }

            ViewBag.Code = normCode;
            ViewBag.Nick = normNick;
            return View();
        }

        // GET: /Game/Play
        public IActionResult Play(string code, string nick)
        {
            var normCode = SessionCodeHelper.Normalize(code);
            var normNick = (nick ?? "").Trim();

            if (!SessionCodeHelper.IsValid(normCode) || string.IsNullOrWhiteSpace(normNick))
            {
                return RedirectToAction(nameof(Join));
            }

            if (!_gameSessionService.SessionExists(normCode))
            {
                TempData["Error"] = "Sesja nie istnieje lub wygasła";
                return RedirectToAction(nameof(Join));
            }

            return View(new PlayViewModel
            {
                AccessCode = normCode,
                Nickname = normNick
            });
        }

        #endregion

        #region Host Actions

        // GET: /Game/HostGame/5
        [Authorize]
        public async Task<IActionResult> HostGame(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Pobierz dane quizu
            var result = await _quizService.GetQuizForGameAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = "Quiz nie został znaleziony";
                return NotFound();
            }

            // Sprawdź uprawnienia
            if (!await _quizService.IsOwnerOrAdminAsync(id, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            var gameQuizDto = result.Data;
            gameQuizDto.AccessCode = SessionCodeHelper.Normalize(gameQuizDto.AccessCode);

            // Inicjuj sesję
            var initResult = _gameSessionService.InitializeSession(
                new StartSessionDto { QuizId = id },
                gameQuizDto,
                userId
            );

            if (!initResult.Success)
            {
                TempData["Error"] = string.Join(", ", initResult.Errors);
                return RedirectToAction("Details", "Quizzes", new { id });
            }

            _logger.LogInformation("User {UserId} hosting quiz {QuizId} with code {Code}",
                userId, id, gameQuizDto.AccessCode);

            return RedirectToAction("LobbyHost", new { code = gameQuizDto.AccessCode });
        }

        // GET: /Game/LobbyHost
        [Authorize]
        public IActionResult LobbyHost(string code)
        {
            var normCode = SessionCodeHelper.Normalize(code);

            if (!SessionCodeHelper.IsValid(normCode))
            {
                TempData["Error"] = "Nieprawidłowy kod sesji";
                return RedirectToAction("Index", "Quizzes");
            }

            if (!_gameSessionService.SessionExists(normCode))
            {
                TempData["Error"] = "Sesja nie istnieje lub wygasła";
                return RedirectToAction("Index", "Quizzes");
            }

            ViewBag.AccessCode = normCode;
            return View();
        }

        // GET: /Game/PlayHost
        [Authorize]
        public IActionResult PlayHost(string code)
        {
            var normCode = SessionCodeHelper.Normalize(code);

            if (!SessionCodeHelper.IsValid(normCode))
            {
                TempData["Error"] = "Nieprawidłowy kod sesji";
                return RedirectToAction("Index", "Quizzes");
            }

            if (!_gameSessionService.SessionExists(normCode))
            {
                TempData["Error"] = "Sesja nie istnieje lub wygasła";
                return RedirectToAction("Index", "Quizzes");
            }

            return View(new PlayHostViewModel { AccessCode = normCode });
        }

        #endregion

    }
}
