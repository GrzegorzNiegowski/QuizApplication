using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuizService _quizService;

        public QuestionService(ApplicationDbContext context, IQuizService quizService)
        {
            _context = context;
            _quizService = quizService;
        }

        public async Task<OperationResult> AddQuestionAsync(AddQuestionViewModel vm, string userId, bool isAdmin)
        {
            if (vm == null) return OperationResult.Fail("Brak danych");
            if (vm.QuizId <= 0) return OperationResult.Fail("Nieprawidłowy identyfikator Quizu");
            if (!await _quizService.IsOwnerOrAdminAsync(vm.QuizId, userId, isAdmin)) return OperationResult.Fail("Brak Uprawnień");

            // walidacja viewmodel (dodatkowa) 
            if (string.IsNullOrWhiteSpace(vm.Content)) return OperationResult.Fail("Treść pytania jest wymagana.");
            var nonEmptyAnswers = vm.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).ToList();
            if (!nonEmptyAnswers.Any()) return OperationResult.Fail("Dodaj co najmniej jedną odpowiedź.");
            if (!nonEmptyAnswers.Any(a => a.IsCorrect)) return OperationResult.Fail("Przynajmniej jedna odpowiedź musi być poprawna.");

            // limit punktów check (spójność z encją)
            if (vm.Points < 0 || vm.Points > 5000) return OperationResult.Fail("Punkty poza dopuszczalnym zakresem.");

            var question = new Question
            {
                QuizId = vm.QuizId,
                Content = vm.Content.Trim(),
                TimeLimitSeconds = vm.TimeLimitSeconds,
                Points = vm.Points
            };

            foreach (var a in nonEmptyAnswers)
            {
                question.Answers.Add(new Answer
                {
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                });
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin)
        {
            var q = await _context.Questions.FirstOrDefaultAsync(x => x.Id == questionId);
            if(q == null)
            {
                return OperationResult.Fail("Pytanie nie istnieje");
            }

            if(!await _quiz_service_check(q.QuizId, userId, isAdmin))
            {
                return OperationResult.Fail("Brak uprawnień");
            }

            _context.Answers.RemoveRange(_context.Answers.Where(a => a.QuestionId == questionId));
            _context.Questions.Remove(q);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult> EditQuestionAsync(EditQuestionViewModel vm, string userId, bool isAdmin)
        {
            if (vm == null) return OperationResult.Fail("Brak danych.");
            if (vm.QuestionId <= 0) return OperationResult.Fail("Nieprawidłowy identyfikator pytania.");

            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == vm.QuestionId);
            if (q == null) return OperationResult.Fail("Pytanie nie istnieje.");

            if (!await _quiz_service_check(vm.QuizId, userId, isAdmin)) return OperationResult.Fail("Brak uprawnień.");

            // podstawowa walidacja
            if (string.IsNullOrWhiteSpace(vm.Content)) return OperationResult.Fail("Treść pytania jest wymagana.");
            var nonEmptyAnswers = vm.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).ToList();
            if (!nonEmptyAnswers.Any()) return OperationResult.Fail("Dodaj co najmniej jedną odpowiedź.");
            if (!nonEmptyAnswers.Any(a => a.IsCorrect)) return OperationResult.Fail("Przynajmniej jedna odpowiedź musi być poprawna.");

            // aktualizujemy pytanie
            q.Content = vm.Content.Trim();
            q.TimeLimitSeconds = vm.TimeLimitSeconds;
            q.Points = vm.Points;

            // proste podejście: usuń stare odpowiedzi i dodaj nowe z VM (łatwe i bezpieczne)
            _context.Answers.RemoveRange(q.Answers);
            q.Answers.Clear();

            foreach (var a in nonEmptyAnswers)
            {
                q.Answers.Add(new Answer
                {
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                });
            }

            _context.Questions.Update(q);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult<EditQuestionViewModel>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin)
        {
            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == questionId);

            if (q == null) return OperationResult<EditQuestionViewModel>.Fail("Pytanie nie istnieje.");

            // check permissions via quiz
            var allowed = await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin);
            if (!allowed) return OperationResult<EditQuestionViewModel>.Fail("Brak uprawnień.");

            var vm = new EditQuestionViewModel
            {
                QuestionId = q.Id,
                QuizId = q.QuizId,
                Content = q.Content,
                TimeLimitSeconds = q.TimeLimitSeconds,
                Points = q.Points,
                Answers = q.Answers.OrderBy(a => a.Id).Select(a => new EditAnswerViewModel
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return OperationResult<EditQuestionViewModel>.Ok(vm);
        }


        // helper aby wywołać quizService.IsOwnerOrAdmin (wydzielone, by uniknąć powtórzeń)
        private async Task<bool> _quiz_service_check(int quizId, string userId, bool isAdmin)
        {
            return await _quizService.IsOwnerOrAdminAsync(quizId, userId, isAdmin);
        }

    }
}
