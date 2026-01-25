using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    /// <summary>
    /// Kontroler obsługujący rozgrywkę quizową
    /// </summary>
    public class GameController : Controller
    {
        private readonly IGameSessionService _gameSessionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GameController> _logger;

        public GameController(
            IGameSessionService gameSessionService,
            ApplicationDbContext context,
            ILogger<GameController> logger)
        {
            _gameSessionService = gameSessionService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Strona dołączania do gry (dla graczy)
        /// </summary>
        [HttpGet]
        public IActionResult Join(string? code = null)
        {
            var model = new JoinGameDto();

            if (!string.IsNullOrEmpty(code))
            {
                model.AccessCode = code.ToUpperInvariant();
            }

            // Jeśli użytkownik jest zalogowany, pobierz jego nick
            if (User.Identity?.IsAuthenticated == true)
            {
                var userName = User.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(userName))
                {
                    model.Nickname = userName;
                }
            }

            return View(model);
        }

        /// <summary>
        /// Lobby hosta - udostępnianie quizu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Host(int quizId)
        {
            // Sprawdź czy użytkownik jest właścicielem quizu
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
            {
                TempData["Error"] = "Quiz nie został znaleziony";
                return RedirectToAction("Index", "Quizzes");
            }

            if (quiz.OwnerId != userId)
            {
                TempData["Error"] = "Nie masz uprawnień do tego quizu";
                return RedirectToAction("Index", "Quizzes");
            }

            if (!quiz.Questions.Any())
            {
                TempData["Error"] = "Quiz musi mieć przynajmniej jedno pytanie";
                return RedirectToAction("Details", "Quizzes", new { id = quizId });
            }

            ViewBag.QuizId = quizId;
            ViewBag.QuizTitle = quiz.Title;
            ViewBag.AccessCode = quiz.AccessCode;
            ViewBag.QuestionCount = quiz.Questions.Count;

            return View();
        }

        /// <summary>
        /// Lobby gracza - oczekiwanie na start
        /// </summary>
        [HttpGet]
        public IActionResult Lobby(Guid sessionId, Guid playerId)
        {
            var session = _gameSessionService.GetSession(sessionId);

            if (session == null)
            {
                TempData["Error"] = "Sesja nie istnieje";
                return RedirectToAction("Join");
            }

            ViewBag.SessionId = sessionId;
            ViewBag.PlayerId = playerId;
            ViewBag.QuizTitle = session.QuizTitle;
            ViewBag.AccessCode = session.AccessCode;

            return View();
        }

        /// <summary>
        /// Widok rozgrywki (wspólny dla hosta i gracza)
        /// </summary>
        [HttpGet]
        public IActionResult Play(Guid sessionId, Guid? playerId = null, bool isHost = false)
        {
            var session = _gameSessionService.GetSession(sessionId);

            if (session == null)
            {
                TempData["Error"] = "Sesja nie istnieje";
                return RedirectToAction("Join");
            }

            ViewBag.SessionId = sessionId;
            ViewBag.PlayerId = playerId;
            ViewBag.IsHost = isHost;
            ViewBag.QuizTitle = session.QuizTitle;
            ViewBag.TotalQuestions = session.TotalQuestions;

            return View();
        }

        /// <summary>
        /// Widok wyników końcowych
        /// </summary>
        [HttpGet]
        public IActionResult Results(Guid sessionId, Guid? playerId = null, bool isHost = false)
        {
            var session = _gameSessionService.GetSession(sessionId);

            if (session == null)
            {
                TempData["Error"] = "Sesja nie istnieje";
                return RedirectToAction("Join");
            }

            ViewBag.SessionId = sessionId;
            ViewBag.PlayerId = playerId;
            ViewBag.IsHost = isHost;
            ViewBag.QuizTitle = session.QuizTitle;

            return View();
        }

        /// <summary>
        /// API: Sprawdź czy kod gry jest prawidłowy
        /// </summary>
        [HttpGet]
        public IActionResult CheckCode(string code)
        {
            var session = _gameSessionService.GetSessionByAccessCode(code.ToUpperInvariant());

            if (session == null)
            {
                return Json(new { valid = false, message = "Nie znaleziono gry o podanym kodzie" });
            }

            if (!session.CanJoin)
            {
                return Json(new { valid = false, message = "Gra już się rozpoczęła" });
            }

            return Json(new
            {
                valid = true,
                quizTitle = session.QuizTitle,
                playerCount = session.ActivePlayerCount
            });
        }
    }
}
