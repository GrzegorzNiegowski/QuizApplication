using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuizApplication.Data;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Services;
using QuizApplication.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Index()
        {
            // odczyt quizId z query string
            var qs = Request.Query["quizId"].FirstOrDefault();
            if (!int.TryParse(qs, out var quizId) || quizId <= 0)
            {
                return BadRequest("Brakuje prawidłowego parametru quizId w query string (np. /Questions?quizId=123).");
            }

            var quizResult = await _quizService.GetQuizWithDetailsAsync(quizId);
            if (!quizResult.Success || quizResult.Data == null)
            {
                return NotFound();
            }

            // zwracamy widok z listą pytań (możesz też zwrócić quizResult.Data jeśli chcesz tytuł + pytania)
            var questions = quizResult.Data.Questions.OrderBy(q => q.Id).ToList();
            return View(questions);
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var result = await _quizService.GetQuizWithDetailsAsync(id);
            if (!result.Success) return NotFound();

            return View(result.Data);
        }

        // GET: Questions/Create?quizId=5
        public async Task<IActionResult> Create(int quizId)
        {
            var quizResult = await _quiz_service_getbyid(quizId);
            if (!quizResult.Success) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _quizService.IsOwnerOrAdminAsync(quizId, userId, User.IsInRole("Admin")))
                return Forbid();

            ViewBag.QuizTitle = quizResult.Data!.Title;
            var vm = new AddQuestionViewModel { QuizId = quizId };
            return View(vm);
        }

        // POST Create — (niezmienione)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddQuestionViewModel vm, string? submit)
        {
            if (!ModelState.IsValid)
            {
                var q = await _quiz_service_getbyid(vm.QuizId);
                ViewBag.QuizTitle = q.Data?.Title ?? "";
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _questionService.AddQuestionAsync(vm, userId, User.IsInRole("Admin"));
            if (!result.Success)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e);
                var q = await _quiz_service_getbyid(vm.QuizId);
                ViewBag.QuizTitle = q.Data?.Title ?? "";
                return View(vm);
            }

            TempData["Success"] = "Pytanie zapisane.";
            if (submit == "finish") return RedirectToAction("Details", "Quizzes", new { id = vm.QuizId });
            return RedirectToAction(nameof(Create), new { quizId = vm.QuizId });
        }

        // GET Edit
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var getResult = await _questionService.GetQuestionForEditAsync(id, userId, User.IsInRole("Admin"));
            if (!getResult.Success) return NotFound(); // lub Forbid jeśli komunikat z serwisu to brak uprawnień

            var vm = getResult.Data!;
            var quiz = await _quiz_service_getbyid(vm.QuizId);
            ViewBag.QuizTitle = quiz.Data?.Title ?? "";
            return View(vm);
        }

        // POST Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditQuestionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var quiz = await _quiz_service_getbyid(vm.QuizId);
                ViewBag.QuizTitle = quiz.Data?.Title ?? "";
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var res = await _questionService.EditQuestionAsync(vm, userId, User.IsInRole("Admin"));
            if (!res.Success)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e);
                var quiz = await _quiz_service_getbyid(vm.QuizId);
                ViewBag.QuizTitle = quiz.Data?.Title ?? "";
                return View(vm);
            }

            return RedirectToAction("Details", "Quizzes", new { id = vm.QuizId });
        }

        // GET Delete (confirm)
        public async Task<IActionResult> Delete(int id)
        {
            // pobierz question by pokazać podsumowanie (tu używamy GetQuestionForEditAsync do wygody)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var getResult = await _questionService.GetQuestionForEditAsync(id, userId, User.IsInRole("Admin"));
            if (!getResult.Success) return NotFound();

            var vm = getResult.Data!;
            var quiz = await _quiz_service_getbyid(vm.QuizId);
            ViewBag.QuizTitle = quiz.Data?.Title ?? "";
            return View(vm);
        }

        // POST Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int questionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var res = await _questionService.DeleteQuestionAsync(questionId, userId, User.IsInRole("Admin"));
            if (!res.Success)
            {
                // nie powinniśmy ujawniać zbyt wiele — przekieruj z błędem
                TempData["Error"] = string.Join("; ", res.Errors);
                return RedirectToAction("Index", "Quizzes");
            }

            // po usunięciu przekieruj do Details quizu - znajdź quizId (można przechować w TempData ale prostsze: niech Delete view ma hidden QuizId i wyśle je)
            // tutaj zakładamy, że w Delete formie przesyłasz pola: questionId i quizId -> ale w tym kontrolerze mamy tylko questionId.
            // prostsze: po usunięciu wróć do Index quizów
            return RedirectToAction("Index", "Quizzes");
        }

        // helper: mała metoda by pobrać quiz
        private async Task<OperationResult<Quiz>> _quiz_service_getbyid(int quizId)
        {
            return await _quizService.GetByIdAsync(quizId);
        }

    }
}
