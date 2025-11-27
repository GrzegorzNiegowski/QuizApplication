using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.Models;

namespace QuizApplication.Controllers
{
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
        public IActionResult Create()
        {
            ViewData["QuizId"] = new SelectList(_context.Quizzes, "Id", "Title");
            return View();
        }

        // POST: Questions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Content,TimeLimitSeconds,ImageUrl,QuizId")] Question question)
        {
            if (ModelState.IsValid)
            {
                _context.Add(question);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["QuizId"] = new SelectList(_context.Quizzes, "Id", "Title", question.QuizId);
            return View(question);
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
    }
}
