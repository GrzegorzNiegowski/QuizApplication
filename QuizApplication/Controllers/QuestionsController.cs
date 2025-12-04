using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QuizApplication.Data;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
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
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Questions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Questions.Include(q => q.Quiz);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // GET: Questions/Create
        public async Task<IActionResult> Create(int quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) 
            {
                return NotFound();
            }
            if(!IsOwnerOrAdmin(quiz))
            {
                return Forbid();
            }

            var vm = new QuestionCreateViewModel
            {
                QuizId = quizId,
                TimeLimitSeconds = 30
            };

            ViewBag.QuizTitle = quiz.Title;
            return View(vm);
        }

        // POST: Questions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionCreateViewModel model, string submit)
        {
            // model validation (DataAnnotations)
            if (!ModelState.IsValid)
            {
                var quizCheck = await _context.Quizzes.FindAsync(model.QuizId);
                ViewBag.QuizTitle = quizCheck?.Title ?? "";
                return View(model);
            }

            // dodatkowa walidacja: co najmniej jedna niepusta odpowiedź
            var nonEmptyAnsers = model.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).ToList();
            if (!nonEmptyAnsers.Any())
            {
                ModelState.AddModelError("", "Dodaj co najmniej jedną odpowiedź");
                var quizCheck = await _context.Quizzes.FindAsync(model.QuizId);
                ViewBag.QuizTitle = quizCheck?.Title ?? "";
                return View(model);
            }

            // co najmniej jedna poprawna odpowiedź spośród niepustych
            if(!nonEmptyAnsers.Any(a => a.IsCorrect))
            {
                ModelState.AddModelError("", "Przynajmniej jedna z wprowadzanych odpowiedzi musi być oznaczona jako poprawna");
                var quizCheck = await _context.Quizzes.FindAsync(model.QuizId);
                ViewBag.QuizTitle = quizCheck?.Title ?? "";
                return View(model);
            }

            // dodatkowe: sprawdź uprawnienia (ponownie, zabezpieczenie)
            var quiz = await _context.Quizzes.FindAsync(model.QuizId);
            if (quiz != null)
            {
                return NotFound();
            }
            if(!IsOwnerOrAdmin(quiz))
            {
                return Forbid();
            }

            var question = new Question
            {
                Content = model.Content,
                TimeLimitSeconds = model.TimeLimitSeconds,
                QuizId = model.QuizId,
            };

            foreach(var a in nonEmptyAnsers)
            {
                var answerEntity = new Answer
                {
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                };
                question.Answers.Add(answerEntity);
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // jeśli user kliknął "Zakończ i pokaż quiz" -> redirect do Details quizu
            if(!string.IsNullOrEmpty(submit) && submit == "finish")
            {
                return RedirectToAction("Details", "QuizzesController", new { id = model.QuizId });
            }
            return RedirectToAction(nameof(Create), new { quizId = model.QuizId });

        }

        // GET: Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }
            ViewData["QuizId"] = new SelectList(_context.Quizzes, "Id", "Title", question.QuizId);
            return View(question);
        }

        // POST: Questions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,TimeLimitSeconds,ImageUrl,QuizId")] Question question)
        {
            if (id != question.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["QuizId"] = new SelectList(_context.Quizzes, "Id", "Title", question.QuizId);
            return View(question);
        }

        // GET: Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }

        private bool IsOwnerOrAdmin(Quiz quiz)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return false;
            if (quiz.OwnerId == userId) return true;
            return User.IsInRole("Admin");
        }


    }
}
