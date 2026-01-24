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
        /// Serwis do zarządzania quizami
        /// </summary>
        public interface IQuizService
        {
            /// <summary>
            /// Tworzy nowy quiz
            /// </summary>
            Task<OperationResult<QuizDetailsDto>> CreateAsync(CreateQuizDto dto);

            /// <summary>
            /// Pobiera szczegóły quizu wraz z pytaniami
            /// </summary>
            Task<OperationResult<QuizDetailsDto>> GetByIdAsync(int id);

            /// <summary>
            /// Pobiera listę quizów użytkownika
            /// </summary>
            Task<OperationResult<List<QuizListDto>>> GetAllForUserAsync(string userId);

            /// <summary>
            /// Aktualizuje quiz
            /// </summary>
            Task<OperationResult> UpdateAsync(EditQuizDto dto, string userId, bool isAdmin);

            /// <summary>
            /// Usuwa quiz
            /// </summary>
            Task<OperationResult> DeleteAsync(int quizId, string userId, bool isAdmin);

            /// <summary>
            /// Sprawdza czy użytkownik jest właścicielem lub adminem
            /// </summary>
            Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin);
        }
    }

