using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;
using System.Threading.Tasks;

namespace QuizApplication.Services
{
    public interface IQuizService
    {
        Task<OperationResult<QuizDetailsDto>> CreateQuizAsync(CreateQuizDto dto);
        Task<OperationResult<QuizDetailsDto>> GetQuizDetailsAsync(int id);
        Task<OperationResult<List<QuizSummaryDto>>> GetAllQuizzesForUserAsync(string userId);
        Task<OperationResult> UpdateQuizTitleAsync(UpdateQuizDto dto, string userId, bool isAdmin);
        Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin);

        // Helpery
        Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin);

        // DLA GRY (Pobiera pełne dane z odpowiedziami do pamięci RAM)
        Task<OperationResult<GameQuizDto>> GetQuizForGameAsync(int id);



    }
}
