using QuizApplication.DTOs;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;
using System.Threading.Tasks;

namespace QuizApplication.Services
{
    public interface IQuizService
    {
        Task<OperationResult<QuizDto>> CreateQuizAsync(CreateQuizDto dto);
        Task<OperationResult<QuizDto>> GetQuizByIdAsync(int id);
        Task<OperationResult<List<QuizDto>>> GetAllQuizzesForUserAsync(string userId);
        Task<OperationResult> UpdateQuizTitleAsync(UpdateQuizDto dto, string userId, bool isAdmin);
        Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin);

        // Pomocnicze
        Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin);
        Task<OperationResult<QuizDto>> GetQuizByAccessCodeAsync(string accessCode);



    }
}
