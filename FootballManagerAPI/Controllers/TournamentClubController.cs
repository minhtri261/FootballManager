using FootballManager.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.API.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class TournamentClubController : ControllerBase
    {
        private readonly TournamentClubService _service;
        private readonly TournamentMatchService _tournamentMatchService;


        public TournamentClubController(TournamentClubService service, TournamentMatchService tournamentMatchService)
        {
            _service = service;
            _tournamentMatchService = tournamentMatchService;
        }

        //Thêm CLB vào giải đấu
        [HttpPost("add")]
        public async Task<IActionResult> AddClub([FromQuery] int tournamentId, [FromQuery] int clubId)
        {
            try
            {
                await _service.AddClubToTournamentAsync(tournamentId, clubId);
                return Ok(new { message = "Thêm CLB vào giải đấu thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //Hiển thị danh sách các trận đấu trong giải đấu
        [HttpGet("{tournamentId}/matches")]
        public async Task<IActionResult> GetMatchesByTournament(int tournamentId)
        {
            try
            {
                var matches = await _tournamentMatchService.GetMatchesByTournamentAsync(tournamentId);
                return Ok(matches);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Tạo lịch thi đấu cho giải đấu
        [HttpPost("{id}/generate-matches")]
        public async Task<IActionResult> GenerateMatches(int id)
        {
            try
            {
                await _tournamentMatchService.GenerateMatchesAsync(id);
                return Ok(new { message = "Tạo lịch thi đấu thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //Xem bảng xếp hạng của giải đấu

    }
}
