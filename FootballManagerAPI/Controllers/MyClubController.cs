using FootballManager.Business.DTOs;
using FootballManager.Business.Services;
using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [ApiController]
    [Route("api/myclub")]
    public class MyClubController : ControllerBase
    {
        private readonly IClubService _service;
        private readonly IMatchService _matchService;
        private readonly RandomResultService randomResultService;
        public MyClubController(IClubService service, IMatchService matchService, RandomResultService randomResultService)
        {
            _service = service;
            _matchService = matchService;
            this.randomResultService = randomResultService;
        }

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

        //Chọn dội hình ra sân cho trận đấu của CLB mình , sau đó hệ thống sẽ tiến hành random kết quả các trận đấu của Round đó
        [HttpPost("lineup")]
        public async Task<IActionResult> SubmitLineup([FromBody] SubmitLineupDto dto)
        {
            //Hàm lưu đội hình ra sân của player
            await _matchService.SaveMatchLineupAsync(dto);

            //Hàm random kết quả các trận đấu trong Round đó
            var matches = await randomResultService.RandomizeResultsAsync(dto.TournamentId);

            //Hàm cập nhật kết quả bảng xếp hạng sau khi có kết quả trận đấu
            foreach (var m in matches)
            {
                await _matchService.ApplySimulationResultAsync(m);
            }
            return Ok(new { message = "Lineup submitted successfully and begin to start Random Result" });
        }

        //Lấy kết quả trận đấu gần nhất của CLB mình
        [HttpGet("last-match-result")]
        public async Task<IActionResult> GetLastMatchResult([FromQuery] int tournamentId)
        {
            var result = await _matchService.MyClubResultLastRound(tournamentId);
            if (result == null) return NotFound("No match results found for your club.");
            return Ok(result);
        }
    }
}
