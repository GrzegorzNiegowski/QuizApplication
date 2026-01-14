using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs;
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
                return View(new List<QuizDto>());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _quizService.GetAllQuizzesForUserAsync(userId);

            // Wysyłamy do widoku listę DTO
            return View(result.Data);
        }

        // GET: /Quizzes/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _quizService.GetQuizByIdAsync(id);
            if (!result.Success) return NotFound();

            // Wysyłamy do widoku DTO
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
            if (!ModelState.IsValid) return View(vm);

            var dto = new CreateQuizDto
            {
                Title = vm.Title,
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
            };

            var result = await _quizService.CreateQuizAsync(dto);

            if (!result.Success)
            {
                result.Errors.ForEach(e => ModelState.AddModelError("", e));
                return View(vm);
            }

            return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
        }

        // GET: /Quizzes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _quizService.GetQuizByIdAsync(id);
            if (!result.Success) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _quizService.IsOwnerOrAdminAsync(id, userId, User.IsInRole("Admin")))
                return Forbid();

            // Przepisanie DTO -> ViewModel
            var vm = new EditQuizViewModel
            {
                Id = result.Data!.Id,
                Title = result.Data.Title
            };

            return View(vm);
        }

        // POST: /Quizzes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditQuizViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var dto = new UpdateQuizDto
            {
                Id = vm.Id,
                Title = vm.Title
            };

            var result = await _quizService.UpdateQuizTitleAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("Admin"));

            if (!result.Success)
            {
                result.Errors.ForEach(e => ModelState.AddModelError("", e));
                return View(vm);
            }

            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        // GET: /Quizzes/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _quizService.GetQuizByIdAsync(id);
            if (!result.Success) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _quizService.IsOwnerOrAdminAsync(id, userId, User.IsInRole("Admin")))
                return Forbid();

            // Widok Delete wyświetla dane, więc wysyłamy DTO
            return View(result.Data);
        }

        // POST: /Quizzes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _quizService.DeleteQuizAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("Admin"));
            if (!result.Success) return Forbid();

            return RedirectToAction(nameof(Index));
        }
    }


}
