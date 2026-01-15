using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Services;
using QuizApplication.Utilities;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    [Authorize]
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly IQuizService _quizService;

        public QuestionsController(IQuestionService questionService, IQuizService quizService)
        {
            _questionService = questionService;
            _quizService = quizService;
        }

        // GET: Questions (Lista pytań dla danego quizu)
        public async Task<IActionResult> Index(int quizId)
        {
            if (quizId <= 0)
                return BadRequest("Wymagane jest podanie poprawnego quizId.");

            // 1. Pobieramy Quiz (serwis zwraca QuizDto, który zawiera już listę Questions)
            var quizResult = await _quizService.GetQuizDetailsAsync(quizId);

            if (!quizResult.Success || quizResult.Data == null)
                return NotFound("Nie znaleziono quizu.");

            // 2. WAŻNE: Sprawdzamy uprawnienia (czy user jest właścicielem quizu)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _quizService.IsOwnerOrAdminAsync(quizId, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            // 3. Opcjonalnie: Przekazujemy tytuł quizu do widoku
            ViewBag.QuizTitle = quizResult.Data.Title;
            ViewBag.QuizId = quizId;

            // 4. Zwracamy samą listę pytań (List<QuestionDto>)
            var questions = quizResult.Data.Questions.OrderBy(q => q.QuestionId).ToList();
            return View(questions);
        }


        // GET: Questions/Create?quizId=5
        public async Task<IActionResult> Create(int quizId)
        {
            var result = await _quizService.GetQuizDetailsAsync(quizId);
            if (!result.Success) return NotFound();

            ViewBag.QuizTitle = result.Data!.Title;
            return View(new AddQuestionViewModel { QuizId = quizId });
        }

        // POST Create — (niezmienione)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddQuestionViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // ViewModel -> DTO
            var dto = new CreateQuestionDto
            {
                QuizId = vm.QuizId,
                Content = vm.Content,
                TimeLimitSeconds = vm.TimeLimitSeconds,
                Points = vm.Points,
                Answers = vm.Answers.Select(a => new CreateAnswerDto { Content = a.Content, IsCorrect = a.IsCorrect }).ToList()
            };

            var result = await _questionService.AddQuestionAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("Admin"));

            if (!result.Success)
            {
                result.Errors.ForEach(e => ModelState.AddModelError("", e));
                // Przywróć tytuł quizu
                var qRes = await _quizService.GetQuizDetailsAsync(vm.QuizId);
                if (qRes.Success) ViewBag.QuizTitle = qRes.Data!.Title;

                return View(vm);
            }

            if (Request.Form["submit"] == "finish")
                return RedirectToAction("Details", "Quizzes", new { id = vm.QuizId });

            return RedirectToAction("Create", new { quizId = vm.QuizId });
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Pobieramy pytanie przez serwis (metoda przyjmuje userId, by sprawdzić uprawnienia wewnątrz)
            var result = await _questionService.GetQuestionForEditAsync(id, userId, User.IsInRole("Admin"));

            if (!result.Success)
            {
                // Jeśli błąd wynika z braku uprawnień lub braku pytania
                return NotFound(string.Join(", ", result.Errors));
            }

            // result.Data to QuestionDto
            return View(result.Data);
        }

        

        // GET Edit
        public async Task<IActionResult> Edit(int id)
        {
            

            var result = await _questionService.GetQuestionForEditAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("Admin"));
            if (!result.Success) return NotFound();

            var dto = result.Data!;
            var quizRes = await _quizService.GetQuizDetailsAsync(dto.QuizId);
            if (quizRes.Success) ViewBag.QuizTitle = quizRes.Data!.Title;

            // DTO -> ViewModel
            var vm = new EditQuestionViewModel
            {
                QuestionId = dto.QuestionId,
                QuizId = dto.QuizId,
                Content = dto.Content,
                TimeLimitSeconds = dto.TimeLimitSeconds,
                Points = dto.Points,
                Answers = dto.Answers.Select(a => new EditAnswerViewModel
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return View(vm);
        }

        // POST Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditQuestionViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new EditQuestionDto
            {
                QuestionId = vm.QuestionId,
                QuizId = vm.QuizId,
                Content = vm.Content,
                TimeLimitSeconds = vm.TimeLimitSeconds,
                Points = vm.Points,
                Answers = vm.Answers.Select(a => new EditAnswerDto { Id = a.Id, Content = a.Content, IsCorrect = a.IsCorrect }).ToList()
            };

            var result = await _questionService.UpdateQuestionAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("Admin"));

            if (!result.Success)
            {
                result.Errors.ForEach(e => ModelState.AddModelError("", e));
                return View(vm);
            }
            return RedirectToAction("Details", "Quizzes", new { id = vm.QuizId });
        }

        // GET Delete (confirm)
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _questionService.GetQuestionForEditAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("Admin"));
            if (!result.Success) return NotFound();

            // Do widoku "Delete" przekazujemy DTO, bo to tylko odczyt
            return View(result.Data);
        }

        // POST Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int questionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Pobierz ID quizu, żeby wiedzieć gdzie wrócić
            var qResult = await _questionService.GetQuestionForEditAsync(questionId, userId, User.IsInRole("Admin"));
            if (!qResult.Success) return RedirectToAction("Index", "Quizzes");

            var result = await _questionService.DeleteQuestionAsync(questionId, userId, User.IsInRole("Admin"));

            if (!result.Success)
            {
                TempData["Error"] = string.Join("; ", result.Errors);
            }

            return RedirectToAction("Details", "Quizzes", new { id = qResult.Data!.QuizId });
        }

        

    }
}
