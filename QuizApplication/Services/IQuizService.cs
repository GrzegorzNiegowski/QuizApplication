using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;
using System.Threading.Tasks;

namespace QuizApplication.Services
{
    
        /// <summary>
        /// Interfejs serwisu zarządzającego quizami
        /// </summary>
        public interface IQuizService
        {
            /// <summary>
            /// Tworzy nowy quiz
            /// </summary>
            Task<OperationResult<QuizDetailsDto>> CreateQuizAsync(CreateQuizDto dto);

            /// <summary>
            /// Pobiera szczegóły quizu wraz z pytaniami i odpowiedziami
            /// </summary>
            Task<OperationResult<QuizDetailsDto>> GetQuizDetailsAsync(int id);

            /// <summary>
            /// Pobiera wszystkie quizy należące do użytkownika
            /// </summary>
            Task<OperationResult<List<QuizSummaryDto>>> GetAllQuizzesForUserAsync(string userId);

            /// <summary>
            /// Aktualizuje tytuł quizu
            /// </summary>
            Task<OperationResult> UpdateQuizTitleAsync(UpdateQuizDto dto, string userId, bool isAdmin);

            /// <summary>
            /// Usuwa quiz wraz ze wszystkimi pytaniami i odpowiedziami
            /// </summary>
            Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin);

            /// <summary>
            /// Sprawdza czy użytkownik jest właścicielem quizu lub administratorem
            /// </summary>
            Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin);



        }
}
