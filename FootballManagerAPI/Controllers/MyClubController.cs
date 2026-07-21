using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [Route("api/myclub")]
    [ApiController]
    public class MyClubController : ControllerBase
    {
        private readonly IMyClubService _myClubService;

        public MyClubController(IMyClubService myClubService)
        {
            _myClubService = myClubService;
        }

        // Thông tin CLB của người chơi
        [HttpGet]
        public async Task<IActionResult> GetMyClub()
        {
            var club = await _myClubService.GetMyClubAsync();
            if (club == null) return NotFound(new { error = "Không tìm thấy CLB của bạn." });
            return Ok(club);
        }

        // Danh sách cầu thủ của CLB mình
        [HttpGet("players")]
        public async Task<IActionResult> GetMyPlayers()
        {
            var players = await _myClubService.GetMyPlayersAsync();
            return Ok(players);
        }

        // Trận đấu tiếp theo chưa đá của CLB mình
        [HttpGet("next-match")]
        public async Task<IActionResult> GetNextMatch()
        {
            var match = await _myClubService.GetNextMatchAsync();
            if (match == null) return NotFound(new { error = "Không có trận đấu nào sắp tới." });
            return Ok(match);
        }

        // Xem đội hình đối thủ trong trận đấu tiếp theo
        [HttpGet("next-opponent-lineup")]
        public async Task<IActionResult> GetNextOpponentLineup()
        {
            var lineup = await _myClubService.GetNextOpponentLineupAsync();
            if (lineup == null) return NotFound(new { error = "Chưa có đội hình đối thủ cho trận tiếp theo." });
            return Ok(lineup);
        }

        // Nộp đội hình ra sân cho trận đấu sắp tới
        [HttpPost("lineup")]
        public async Task<IActionResult> SubmitLineup([FromBody] SubmitLineupDto dto)
        {
            try
            {
                await _myClubService.SubmitLineupAsync(dto);
                return Ok(new { message = "Đã lưu đội hình." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Kết quả trận đấu gần nhất của CLB mình
        [HttpGet("last-result")]
        public async Task<IActionResult> GetLastMatchResult()
        {
            var result = await _myClubService.GetLastMatchResultAsync();
            if (result == null) return NotFound(new { error = "Chưa có trận nào được đá." });
            return Ok(result);
        }
    }
}
