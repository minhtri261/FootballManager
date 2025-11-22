using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [ApiController]
    [Route("api/myclub")]
    public class MyClubController : ControllerBase
    {
        private readonly IClubService _service;
        public MyClubController(IClubService service) => _service = service;

        // Lấy thông tin CLB của bạn (theo MyClubId trong appsettings)
        [HttpGet]
        public async Task<IActionResult> GetMyClub()
        {
            var club = await _service.GetWithPlayersAsync();
            if (club == null) return NotFound("Club not found.");
            return Ok(club);
        }

        // Lấy danh sách cầu thủ của CLB mình
        [HttpGet("players")]
        public async Task<IActionResult> GetMyPlayers()
        {
            var players = await _service.GetMyPlayersAsync();
            return Ok(players);
        }

        // Lấy chi tiết cầu thủ trong CLB mình
        [HttpGet("players/{id}")]
        public async Task<IActionResult> GetPlayerDetail(int id)
        {
            var player = await _service.GetPlayerDetailAsync(id);
            if (player == null) return NotFound("Player not found or not in your club.");
            return Ok(player);
        }
    }
}
