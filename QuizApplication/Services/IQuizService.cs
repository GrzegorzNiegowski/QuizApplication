using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public interface IQuizService
    {
        Task<OperationResult<Quiz>> CreateQuizAsync(CreateQuizViewModel vm, string ownerId);
        Task<OperationResult<Quiz>> GetQuizWithDetailsAsync(int id);
        Task<OperationResult> UpdateTitleAsync(int quizId, string newTitle, string userId, bool isAdmin);
        Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin);
        Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin);
        Task<OperationResult<Quiz>> GetByIdAsync(int id);


        //
        Task<OperationResult<List<Quiz>>> GetQuizzesForUserAsync(string userId);



    }
}
