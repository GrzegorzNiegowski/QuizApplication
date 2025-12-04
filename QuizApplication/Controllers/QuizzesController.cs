using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.Models;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    [Authorize]
    public class QuizzesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuizzesController(ApplicationDbContext context)
        {
            _context = context;
        }


        [AllowAnonymous]
        // GET: Quizzes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Quizzes.Include(q => q.Owner);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Quizzes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Owner).Include(q => q.Questions).ThenInclude(qn => qn.Answers)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (quiz == null)
            {
                return NotFound();
            }

            return View(quiz);
        }

        // GET: Quizzes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Quizzes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title")] Quiz quiz)
        {
            if(!ModelState.IsValid)
            {
                return View(quiz);
            }

            quiz.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            quiz.CreatedAt = DateTime.UtcNow;
            quiz.AccessCode = await GenerateUniqueAccessCodeAsync();
            _context.Add(quiz);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new {id = quiz.Id});
            
            
        }

        // GET: Quizzes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
            {
                return NotFound();
            }

            if(!IsOwnerOrAdmin(quiz))
            {
                return Forbid();
            }

            return View(quiz);
        }

        // POST: Quizzes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Title")] Quiz editedQuiz)
        {

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
            {
                return NotFound();
            }
            
            if(!IsOwnerOrAdmin(quiz))
            {
                return Forbid();
            }

            if(!ModelState.IsValid)
            {
                return View(quiz);
            }

            try
            {
                quiz.Title = editedQuiz.Title;
                _context.Update(quiz);
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException)
            {
                if (QuizExists(quiz.Id))
                {
                    return NotFound();

                }
                else throw;
            }

            return RedirectToAction(nameof(Details), new { id = quiz.Id });
        }

        // GET: Quizzes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (quiz == null)
            {
                return NotFound();
            }

            if(!IsOwnerOrAdmin(quiz))
            {
                return Forbid();
            }

            return View(quiz);
        }

        // POST: Quizzes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
            {
                return NotFound();
            }

            if (!IsOwnerOrAdmin(quiz))
            {
                return Forbid();
            }

            var questions = _context.Questions.Where(q => q.QuizId == quiz.Id);
            _context.Answers.RemoveRange(_context.Answers.Where(a => a.Question.QuizId == quiz.Id));
            _context.Questions.RemoveRange(questions);

            _context.Quizzes.Remove(quiz);
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool QuizExists(int id)
        {
            return _context.Quizzes.Any(e => e.Id == id);
        }


        private bool IsOwnerOrAdmin(Quiz quiz)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null)
            {
                return false;
            }
            if(quiz.OwnerId == userId)
            {
                return true;
            }
            return User.IsInRole("Admin");
        }

        private async Task<string> GenerateUniqueAccessCodeAsync()
        {
            string code;
            do
            {
                code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            } while (await _context.Quizzes.AnyAsync(q => q.AccessCode == code));
            return code;
        }


    }
}
