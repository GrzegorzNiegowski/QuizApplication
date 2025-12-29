using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    [Authorize]
    public class QuizzesController : Controller
    {
        private readonly IQuizService _quizService;

        public QuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }



        //Wszystkie quizy
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return View(Enumerable.Empty<Quiz>());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _quizService.GetQuizzesForUserAsync(userId);

            return View(result.Data ?? Enumerable.Empty<Quiz>());
        }

        // GET: /Quizzes/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _quizService.GetQuizWithDetailsAsync(id);
            if (!result.Success) return NotFound();

            return View(result.Data);
        }

        // GET: /Quizzes/Create
        public IActionResult Create()
        {
            return View(new CreateQuizViewModel());
        }

        // POST: /Quizzes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuizViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Create",vm);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _quizService.CreateQuizAsync(vm, userId);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err);

                return View("Create",vm);
            }

            return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
        }

        // GET: /Quizzes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var quizResult = await _quizService.GetByIdAsync(id);
            if (!quizResult.Success) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _quizService.IsOwnerOrAdminAsync(id, userId, User.IsInRole("Admin")))
                return Forbid();

            var vm = new CreateQuizViewModel
            {
                Title = quizResult.Data!.Title
            };

            return View("Edit",vm);
        }

        // POST: /Quizzes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateQuizViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Edit",vm);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _quizService.UpdateTitleAsync(
                id,
                vm.Title,
                userId,
                User.IsInRole("Admin")
            );

            if (!result.Success)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err);

                return View("Edit", vm);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Quizzes/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var quizResult = await _quizService.GetByIdAsync(id);
            if (!quizResult.Success) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _quizService.IsOwnerOrAdminAsync(id, userId, User.IsInRole("Admin")))
                return Forbid();

            return View(quizResult.Data);
        }

        // POST: /Quizzes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _quizService.DeleteQuizAsync(
                id,
                userId,
                User.IsInRole("Admin")
            );

            if (!result.Success)
                return Forbid();

            return RedirectToAction(nameof(Index));
        }
    }


}
