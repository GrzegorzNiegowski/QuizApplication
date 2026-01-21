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
        private readonly ILogger<GameController> _logger;

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

        // GET: /api/game/validate-join - API do walidacji przed dołączeniem
        [HttpGet("/api/game/validate-join")]
        public IActionResult ValidateJoin(string code, string nick)
        {
            var normCode = SessionCodeHelper.Normalize(code);
            var normNick = (nick ?? "").Trim();

            // Walidacja kodu
            if (!SessionCodeHelper.IsValid(normCode))
            {
                return Json(new { success = false, error = "Nieprawidłowy kod gry" });
            }

            // Sprawdź czy sesja istnieje
            if (!_gameSessionService.SessionExists(normCode))
            {
                return Json(new { success = false, error = "Gra o podanym kodzie nie istnieje" });
            }

            // Sprawdź czy gra już trwa
            if (_gameSessionService.IsGameInProgress(normCode))
            {
                return Json(new { success = false, error = "Gra już trwa, nie można dołączyć" });
            }

            // Walidacja nicku
            if (string.IsNullOrWhiteSpace(normNick))
            {
                return Json(new { success = false, error = "Podaj swój nick" });
            }

            if (normNick.Length > 20)
            {
                return Json(new { success = false, error = "Nick może mieć maksymalnie 20 znaków" });
            }

            // Sprawdź czy nick jest zajęty
            if (_gameSessionService.IsNicknameTaken(normCode, normNick))
            {
                return Json(new { success = false, error = "Ten nick jest już zajęty, wybierz inny" });
            }

            return Json(new { success = true });
        }

        // GET: /Game/Lobby
        public IActionResult Lobby(string code, string nick)
        {
            var normCode = SessionCodeHelper.Normalize(code);
            var normNick = (nick ?? "").Trim();

            if (!SessionCodeHelper.IsValid(normCode) || string.IsNullOrWhiteSpace(normNick))
            {
                TempData["Error"] = "Nieprawidłowe dane";
                return RedirectToAction(nameof(Join));
            }

            if (!_gameSessionService.SessionExists(normCode))
            {
                TempData["Error"] = "Sesja nie istnieje lub wygasła";
                return RedirectToAction(nameof(Join));
            }

            // Sprawdź czy gra już trwa
            if (_gameSessionService.IsGameInProgress(normCode))
            {
                TempData["Error"] = "Gra już trwa, nie można dołączyć";
                return RedirectToAction(nameof(Join));
            }

            // Sprawdź czy nick jest zajęty (podwójna walidacja)
            if (_gameSessionService.IsNicknameTaken(normCode, normNick))
            {
                TempData["Error"] = "Ten nick jest już zajęty";
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

            // Sprawdź czy gracz jest w sesji
            if (!_gameSessionService.IsPlayerInSessionByNickname(normCode, normNick))
            {
                TempData["Error"] = "Nie jesteś uczestnikiem tej gry";
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
