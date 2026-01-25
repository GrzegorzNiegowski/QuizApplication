using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Services;

namespace QuizApplication.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IGameSessionService _sessionService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
        IGameSessionService sessionService,
        ILogger<HealthController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            });
        }

        /// <summary>
        /// Get active game sessions statistics (Admin only)
        /// </summary>
        /// 
        /*
        [HttpGet("sessions")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetActiveSessions()
        {
            try
            {
                var stats = _sessionService.GetSessionStatistics();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session statistics");
                return StatusCode(500, new { error = "Failed to retrieve session statistics" });
            }
        }
        */

    }
}
