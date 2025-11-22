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

        [HttpGet("state")]
        public async Task<IActionResult> GetState()
        {
            var state = await _gameService.GetStateAsync();
            return Ok(new
            {
                state = new
                {
                    state.CurrentSeason,
                    state.CurrentPhase
                }
            });
        }

        [HttpPost("next-phase")]
        public async Task<IActionResult> NextPhase()
        {
            var state = await _gameService.NextPhaseAsync();
            return Ok(new
            {
                message = "Chuyển sang giai đoạn tiếp theo thành công",
                state = new
                {
                    state.CurrentSeason,
                    state.CurrentPhase
                }
            });
        }
     }
}
