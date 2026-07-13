using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Mono.TextTemplating;

namespace FootballManagerAPI.Controllers
{
    [ApiController]
    [Route("api/game")]
    public class GameController : ControllerBase
    {
        private readonly IGameStateService _gameService;

        public GameController(IGameStateService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost("next-week")]
        public async Task<IActionResult> NextWeek()
        {
            try
            {
                var result = await _gameService.AdvanceNextWeekAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
