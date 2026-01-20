namespace QuizApplication.Utilities
{
    public static class GameConstants
    {

        // Timeouty sesji
        public const int SessionCleanupIntervalMinutes = 10;
        public const int InactiveSessionTimeoutHours = 2;
        public const int CompletedGameTimeoutMinutes = 30;

        // Timing gry
        public const int QuestionTransitionSeconds = 3;
        public const int TimerUpdateIntervalMs = 200;
        public const int TimerToleranceSeconds = 1; // Tolerancja dla opóźnień sieciowych

        // Limity
        public const int MaxPlayersPerSession = 100;
        public const int MaxSessionCodeLength = 12;
        public const int MinSessionCodeLength = 4;
        public const int MaxPlayerNameLength = 20;

        // SignalR
        public const int SignalRMaxMessageSizeBytes = 102400; // 100 KB
        public const int SignalRStreamBufferCapacity = 10;
    }
}
