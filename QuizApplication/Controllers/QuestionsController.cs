using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    /// <summary>
    /// Kontroler do zarządzania pytaniami
    /// </summary>
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

        /// <summary>
        /// Formularz dodawania pytania (GET)
        /// </summary>
        public async Task<IActionResult> Create(int quizId)
        {
            if (!await _quizService.IsOwnerOrAdminAsync(quizId, GetUserId(), IsAdmin()))
                return Forbid();

            var quizResult = await _quizService.GetByIdAsync(quizId);
            if (!quizResult.Success)
                return NotFound();

            ViewBag.QuizTitle = quizResult.Data!.Title;
            return View(new CreateQuestionDto { QuizId = quizId });
        }

        /// <summary>
        /// Dodawanie pytania (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuestionDto dto)
        {
            if (!ModelState.IsValid)
            {
                await SetQuizTitle(dto.QuizId);
                return View(dto);
            }

            var result = await _questionService.CreateAsync(dto, GetUserId(), IsAdmin());

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
                await SetQuizTitle(dto.QuizId);
                return View(dto);
            }

            // Sprawdź który przycisk został kliknięty
            if (Request.Form["action"] == "finish")
                return RedirectToAction("Details", "Quizzes", new { id = dto.QuizId });

            return RedirectToAction(nameof(Create), new { quizId = dto.QuizId });
        }

        /// <summary>
        /// Formularz edycji pytania (GET)
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _questionService.GetForEditAsync(id, GetUserId(), IsAdmin());
            if (!result.Success)
                return NotFound();

            await SetQuizTitle(result.Data!.QuizId);
            return View(result.Data);
        }

        /// <summary>
        /// Edycja pytania (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditQuestionDto dto)
        {
            if (!ModelState.IsValid)
            {
                await SetQuizTitle(dto.QuizId);
                return View(dto);
            }

            var result = await _questionService.UpdateAsync(dto, GetUserId(), IsAdmin());

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
                await SetQuizTitle(dto.QuizId);
                return View(dto);
            }

            return RedirectToAction("Details", "Quizzes", new { id = dto.QuizId });
        }

        /// <summary>
        /// Potwierdzenie usunięcia pytania (GET)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _questionService.GetForEditAsync(id, GetUserId(), IsAdmin());
            if (!result.Success)
                return NotFound();

            return View(result.Data);
        }

        /// <summary>
        /// Usunięcie pytania (POST)
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Pobierz QuizId przed usunięciem
            var question = await _questionService.GetForEditAsync(id, GetUserId(), IsAdmin());
            if (!question.Success)
                return RedirectToAction("Index", "Quizzes");

            var quizId = question.Data!.QuizId;
            var result = await _questionService.DeleteAsync(id, GetUserId(), IsAdmin());

            if (!result.Success)
                TempData["Error"] = string.Join("; ", result.Errors);

            return RedirectToAction("Details", "Quizzes", new { id = quizId });
        }

        #region Metody pomocnicze

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin() => User.IsInRole("Admin");

        private async Task SetQuizTitle(int quizId)
        {
            var result = await _quizService.GetByIdAsync(quizId);
            if (result.Success)
                ViewBag.QuizTitle = result.Data!.Title;
        }

        #endregion
    }
}
