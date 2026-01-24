using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    /// <summary>
    /// Kontroler do zarządzania quizami
    /// </summary>
    [Authorize]
    public class QuizzesController : Controller
    {
        private readonly IQuizService _quizService;

        public QuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        /// <summary>
        /// Wyświetla listę quizów użytkownika
        /// </summary>
        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return View(new List<QuizListDto>());

            var result = await _quizService.GetAllForUserAsync(GetUserId());
            return View(result.Data ?? new List<QuizListDto>());
        }

        /// <summary>
        /// Wyświetla szczegóły quizu
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var result = await _quizService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound();

            return View(result.Data);
        }

        /// <summary>
        /// Formularz tworzenia quizu (GET)
        /// </summary>
        public IActionResult Create()
        {
            return View(new CreateQuizDto());
        }

        /// <summary>
        /// Tworzenie quizu (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuizDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            dto.OwnerId = GetUserId();
            var result = await _quizService.CreateAsync(dto);

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
                return View(dto);
            }

            return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
        }

        /// <summary>
        /// Formularz edycji quizu (GET)
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _quizService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound();

            if (!await _quizService.IsOwnerOrAdminAsync(id, GetUserId(), IsAdmin()))
                return Forbid();

            return View(new EditQuizDto { Id = result.Data!.Id, Title = result.Data.Title });
        }

        /// <summary>
        /// Edycja quizu (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditQuizDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _quizService.UpdateAsync(dto, GetUserId(), IsAdmin());

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
                return View(dto);
            }

            return RedirectToAction(nameof(Details), new { id = dto.Id });
        }

        /// <summary>
        /// Potwierdzenie usunięcia quizu (GET)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _quizService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound();

            if (!await _quizService.IsOwnerOrAdminAsync(id, GetUserId(), IsAdmin()))
                return Forbid();

            return View(result.Data);
        }

        /// <summary>
        /// Usunięcie quizu (POST)
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _quizService.DeleteAsync(id, GetUserId(), IsAdmin());
            if (!result.Success)
                return Forbid();

            return RedirectToAction(nameof(Index));
        }

        #region Metody pomocnicze

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin() => User.IsInRole("Admin");

        #endregion
    }

}
