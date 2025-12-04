using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly IMatchService _matchService;

        public MatchController(IMatchService matchService)
        {
            _matchService = matchService;
        }

        //Xem đội hình đối thủ tiếp theo ra sân trong trận đấu của CLB mình
        [HttpGet]
        public async Task<IActionResult> GetOpponentLineup([FromQuery] int matchId)
        {
            try
            {
                var lineup = await _matchService.GetOpponentLineupAsync(matchId);
                return Ok(lineup);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //Xem tất cả các cặp đấu của Round hiện tại trong giải đấu
        [HttpGet("current-round-matches")]
        public async Task<IActionResult> GetCurrentRoundMatches([FromQuery] int tournamentId)
        {
            try
            {
                var matches = await _matchService.GetCurrentRoundMatchesAsync(tournamentId);
                return Ok(matches);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //Kết quả tất cả các trận đấu của Round vừa đá xong trong giải đấu
        [HttpGet("last-round-results")]
        public async Task<IActionResult> GetLastRoundResults([FromQuery] int tournamentId)
        {
            try
            {
                var results = await _matchService.GetLastRoundResultsAsync(tournamentId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //API chuyển trận đấu tiếp theo
        [HttpPost("next-match")]
        public async Task<IActionResult> AdvanceToNextMatch([FromQuery] int seasonNumber)
        {
            try
            {
                await _matchService.PrepareRoundForAllTournamentAsync(seasonNumber);
                return Ok(new { message = "Chuyển sang trận đấu tiếp theo thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
