using FootballManager.Web.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerMVC.Controllers
{
    public class MyTeamController : Controller
    {
        private readonly ApiClient _api;
        public MyTeamController(ApiClient api)
        {
            _api = api;
        }

        // Trang chính - thông tin CLB
        public async Task<IActionResult> Index()
        {
            var club = await _api.GetAsync<Club>("myclub");
            return View(club);
        }

        // Danh sách cầu thủ
        public async Task<IActionResult> Players()
        {
            var players = await _api.GetListAsync<Footballer>("myclub/players");
            return View(players);
        }

        // Chi tiết cầu thủ
        public async Task<IActionResult> Detail(int id)
        {
            var player = await _api.GetAsync<Footballer>($"myclub/players/{id}");
            var club = await _api.GetAsync<Club>("myclub");
            ViewBag.MyClubId = club?.Id ?? 3;

            // ✅ Gọi API lấy danh sách đề nghị chuyển nhượng
            List<TransferOfferModel> offers = new();
            try
            {
                var resp = await _api.GetListAsync<TransferOfferModel>($"transfer/pending-transfers?footballerId={id}");
                if (resp != null)
                    offers = resp.ToList();
            }
            catch
            {
                // Nếu API trả 404/null/exception --> giữ offers rỗng
                offers = new List<TransferOfferModel>();
            }

            ViewBag.Offers = offers;
            return View(player);
        }

        [HttpPost]
        public async Task<IActionResult> RenewContract(int footballerId, int additionalYears = 1)
        {
            // Lấy ClubId hiện tại của người chơi
            var myClub = await _api.GetAsync<Club>("myclub");
            if (myClub == null)
            {
                TempData["Error"] = "Không xác định được CLB của bạn.";
                return RedirectToAction("Detail", new { id = footballerId });
            }

            var apiUrl = $"transfer/renew-contract?clubId={myClub.Id}&footballerId={footballerId}&additionalYears={additionalYears}";
            var result = await _api.PostAsync(apiUrl);
            if (result.IsSuccessStatusCode)
                TempData["Success"] = $"✅ Đã gửi yêu cầu gia hạn {additionalYears} năm.";
            else
                TempData["Error"] = "❌ Lỗi khi gửi yêu cầu gia hạn.";

            return RedirectToAction("Detail", new { id = footballerId });
        }

        [HttpPost]
        public async Task<IActionResult> StartGame()
        {
            // 1️⃣ Gọi API chuyển giai đoạn
            var nextPhaseResponse = await _api.PostAsync("game/next-phase");

            if (!nextPhaseResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "❌ Lỗi khi bắt đầu trò chơi.";
                return RedirectToAction("Index");
            }

            // 2️⃣ Sau khi chuyển giai đoạn thành công → gọi API cho BOT gửi đề nghị
            var botOfferResponse = await _api.PostAsync("transfer/bot-send-offers");

            if (!botOfferResponse.IsSuccessStatusCode)
            {
                TempData["Warning"] = "⚠️ Trò chơi bắt đầu nhưng BOT chưa gửi đề nghị.";
            }
            else
            {
                TempData["Success"] = "🎮 Trò chơi bắt đầu! Các CLB BOT đã gửi đề nghị thành công.";
            }

            // 3️⃣ Refresh lại UI
            return RedirectToAction("Index");
        }


    }
}
