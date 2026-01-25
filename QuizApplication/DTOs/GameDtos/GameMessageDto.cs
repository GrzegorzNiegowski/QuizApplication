namespace QuizApplication.DTOs.GameDtos
{
    // === Komunikaty systemowe ===

    /// <summary>
    /// Komunikat o błędzie/informacji
    /// </summary>
    public class GameMessageDto
    {
        public string Type { get; set; } = "info"; // info, warning, error
        public string Message { get; set; } = string.Empty;
    }

}
