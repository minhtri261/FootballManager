using FootballManager.Web.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerMVC.Controllers
{
    public class MyMatchController : Controller
    {
        private readonly ApiClient _api;
        public MyMatchController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int tournamentId)
        {
            if (tournamentId <= 0)
            return BadRequest("Missing tournamentId");

            var roundState = await _api.GetAsync<RoundStateDto>(
                $"match/round-state?tournamentId={tournamentId}"
            );

            if (roundState.State == "WaitingForLineup")
                return RedirectToAction("Lineup", new { tournamentId });

            return RedirectToAction("MyResult", new { tournamentId });
        }


        public async Task<IActionResult> Lineup(int tournamentId)
        {
            var matches = await _api.GetAsync<List<Match>>(
                $"match/current-round-matches?tournamentId={tournamentId}"
            );
            ViewBag.CurrentTournamentId = tournamentId;
            var myClubId = 3;

            // Tìm trận của mình
            var match = matches?.FirstOrDefault(m => m.HomeClubId == myClubId || m.AwayClubId == myClubId);

            // THÊM ĐOẠN NÀY ĐỂ KIỂM TRA
            if (match != null && match.MatchId <= 0)
            {
                return Content($"Lỗi: Tìm thấy trận đấu nhưng ID trận đấu lại là {match.MatchId}. Kiểm tra lại DB hoặc API mapping!");
            }

            // Nếu thực sự vòng này mình không có lịch thi đấu (ví dụ giải lẻ đội)
            if (match == null)
            {
                // Kiểm tra xem vòng này đã được xử lý kết quả cho Bot chưa
                var roundState = await _api.GetAsync<RoundStateDto>($"match/round-state?tournamentId={tournamentId}");

                if (roundState.State == "WaitingForLineup")
                {
                    // Chỉ khi đang chờ và mình không có trận, mới cho phép random các trận khác
                    await _api.PostAsync($"myclub/randomize-no-match-round?tournamentId={tournamentId}");
                }
                return RedirectToAction("RoundResult", new { tournamentId });
            }

            // Nếu có trận nhưng đã đá rồi
            if (match.IsPlayed)
            {
                return RedirectToAction("MyResult", new { tournamentId });
            }

            // NẾU CÓ TRẬN VÀ CHƯA ĐÁ -> HIỆN VIEW CHỌN ĐỘI HÌNH
            try
            {
                var opponent = await _api.GetAsync<OpponentLineupDto>($"match/opponent-lineup?matchId={match.MatchId}");
                var myPlayers = await _api.GetAsync<List<Footballer>>("myclub/players");
                return View(new MyMatchViewModel { Match = match, OpponentLineup = opponent, MyPlayers = myPlayers });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> SubmitLineup(SubmitLineupDto dto)
        {
            try
            {
                // Kiểm tra data trước khi gửi
                if (dto.PlayerIds == null || dto.PlayerIds.Count < 7)
                    return BadRequest("Chưa chọn đủ cầu thủ");

                await _api.PostAsync("myclub/lineup", dto);
                return RedirectToAction("MyResult", new { tournamentId = dto.TournamentId });
            }
            catch (Exception ex)
            {
                // Ghi log ex để xem API trả về 400 vì lý do gì (Validation error?)
                return Content($"Lỗi khi gọi API: {ex.Message}");
            }
        }


        public async Task<IActionResult> MyResult(int tournamentId)
        {
            var myResult = await _api.GetAsync<MyMatchResultDto>(
                $"myclub/last-match-result?tournamentId={tournamentId}"
            );
            ViewBag.TournamentId = tournamentId;
            if (myResult == null)
            {
                // Bạn có thể redirect về trang chờ hoặc hiển thị một view thông báo đang xử lý
                return Content("Trận đấu đang được hệ thống xử lý, vui lòng đợi giây lát và F5...");
            }
            return View("MyResult", myResult);
        }

        public async Task<IActionResult> RoundResult(int tournamentId)
        {
            var results = await _api.GetAsync<List<MatchResultDto>>(
                $"match/last-round-results?tournamentId={tournamentId}"
            );

            var standings = await _api.GetAsync<List<StandingDto>>(
                $"admin/TournamentClub/{tournamentId}/standings"
            );
            ViewBag.TournamentId = tournamentId;
            return View(new RoundResultViewModel
            {
                TournamentId = tournamentId,
                Matches = results,
                Standings = standings
            });
        }

    }
}
