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
    }
}
