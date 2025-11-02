using FootballManager.Web.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace FootballManagerMVC.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ApiClient _api;
        private const int PageSize = 20;

        public TournamentsController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            var list = await _api.GetListAsync<Tournament>("tournaments");
            ViewBag.PageNumber = pageNumber;
            return View(list);
        }

        // ------------------ DETAILS ------------------
        public async Task<IActionResult> TournamentDetails(int id)
        {
            var t = await _api.GetAsync<Tournament>($"tournaments/{id}/clubs");
            return t == null ? NotFound() : View(t);
        }

        // ------------------ CREATE ------------------
        public IActionResult AdminCreate()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminCreate(Tournament t)
        {
            if (!ModelState.IsValid) return View(t);
            if (Request.Form.TryGetValue("RewardByRank", out var rewardJson))
            {
                try
                {
                    t.RewardByRank = JsonSerializer.Deserialize<List<RewardRank>>(rewardJson!) ?? new();
                }
                catch
                {
                    t.RewardByRank = new();
                }
            }
            await _api.PostAsync("tournaments", t);
            return RedirectToAction(nameof(Index));
        }

        // ------------------ EDIT ------------------
        public async Task<IActionResult> AdminEdit(int id)
        {
            var t = await _api.GetAsync<Tournament>($"tournaments/{id}");
            return t == null ? NotFound() : View(t);
        }

        [HttpPost]
        public async Task<IActionResult> AdminEdit(int id, Tournament t)
        {
            if (id != t.Id) return BadRequest();
            if (Request.Form.TryGetValue("RewardByRank", out var rewardJson))
            {
                try
                {
                    t.RewardByRank = JsonSerializer.Deserialize<List<RewardRank>>(rewardJson!) ?? new();
                }
                catch
                {
                    t.RewardByRank = new();
                }
            }
            await _api.PutAsync($"tournaments/{id}", t);
            return RedirectToAction(nameof(Index));
        }

        // ------------------ DELETE ------------------
        public async Task<IActionResult> AdminDelete(int id)
        {
            var t = await _api.GetAsync<Tournament>($"tournaments/{id}");
            return t == null ? NotFound() : View(t);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _api.DeleteAsync($"tournaments/{id}");
            return RedirectToAction(nameof(Index));
        }

        // Chi tiết các CLB trong giải đấu
        [HttpGet]
        public async Task<IActionResult> ListClubInTournament(int id)
        {
            var tournament = await _api.GetAsync<Tournament>($"tournament/{id}/clubs");
            if (tournament == null)
                return NotFound();

            return View(tournament);
        }

        // Thêm CLB vào giải đấu
        [HttpPost]
        public async Task<IActionResult> AdminAddClub(int tournamentId, int clubId)
        {
            var response = await _api.PostAsync<object>(
                $"tournamentclub/add?tournamentId={tournamentId}&clubId={clubId}",
                null
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Thêm CLB thất bại: {error}";
            }
            else
            {
                TempData["Success"] = "Thêm CLB thành công!";
            }

            // Trả về lại trang chi tiết
            return RedirectToAction(nameof(TournamentDetails), new { id = tournamentId });
        }

        // ------------------ GENERATE MATCHES ------------------
        [HttpPost]
        public async Task<IActionResult> GenerateMatches(int id)
        {
            var response = await _api.PostAsync<object>($"tournamentclub/{id}/generate-matches", null);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Lỗi khi tạo lịch: {error}";
            }
            else
            {
                TempData["Success"] = "Tạo lịch thi đấu thành công!";
            }

            return RedirectToAction(nameof(TournamentDetails), new { id });
        }

        // Danh sách các trận đấu trong giải đấu
        // Danh sách các trận đấu trong giải đấu
        public async Task<IActionResult> ListMatches(int id)
        {
            var tournament = await _api.GetAsync<Tournament>($"tournaments/{id}/clubs");
            if (tournament == null)
                return NotFound();

            var matches = await _api.GetListAsync<Match>($"tournamentclub/{id}/matches");
            tournament.Matches = matches?.ToList() ?? new();

            return View("TournamentDetails", tournament);
        }

    }
}

