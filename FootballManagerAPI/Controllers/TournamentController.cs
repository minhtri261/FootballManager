using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TournamentController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;

        public TournamentController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }

        // Xem chi tiết 1 giải đấu: BXH (League/C1 theo bảng) hoặc bracket (Cup/C1 knockout)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var detail = await _tournamentService.GetTournamentDetailAsync(id);
            if (detail == null) return NotFound(new { error = "Không tìm thấy giải đấu." });
            return Ok(detail);
        }
    }
}
