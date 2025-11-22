using FootballManager.Web.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerMVC.Controllers
{
    public class TransferMarketController : Controller
    {
        private readonly ApiClient _api;
        public TransferMarketController(ApiClient api)
        {
            _api = api;
        }

        // Trang chính: danh sách cầu thủ trên thị trường
        public async Task<IActionResult> Index()
        {
            var myClub = await _api.GetAsync<Club>("myclub");
            int myClubId = myClub.Id;
            var list = await _api.GetListAsync<FootballerDto>("transfer/market");
            var filtered = list.Where(f => f.ClubName == null || f.ClubName != myClub.Name).ToList();

            ViewBag.MyClubId = myClubId;
            return View(filtered);
        }

        // Gửi đề nghị chiêu mộ cầu thủ tự do
        [HttpPost]
        public async Task<IActionResult> FreeAgentOffer(TransferOfferModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            var myClub = await _api.GetAsync<Club>("myclub");
            model.ToClubId = myClub.Id;

            var url = $"transfer/free-agent-offer?clubId={model.ToClubId}&footballerId={model.FootballerId}&contractYears={model.ContractYears}";
            var resp = await _api.PostAsync(url); if (resp.IsSuccessStatusCode)
                TempData["Message"] = "Đề nghị đã gửi thành công!";
            else
                TempData["Error"] = "Không thể gửi đề nghị.";

            return RedirectToAction("Index");
        }

        // Gửi đề nghị mua cầu thủ CLB khác
        [HttpPost]
        public async Task<IActionResult> TransferOffer(TransferOfferModel model)
        {
            if (!ModelState.IsValid) return BadRequest();
            var myClub = await _api.GetAsync<Club>("myclub");
            var url = $"transfer/transfer-offer?fromClubId={model.FromClubId}&footballerId={model.FootballerId}&toClubId={myClub.Id}&contractYears={model.ContractYears}";
            var resp = await _api.PostAsync(url);
            TempData["Message"] = resp.IsSuccessStatusCode
                ? "Đề nghị chuyển nhượng đã gửi!"
                : "Gửi đề nghị thất bại.";


            return RedirectToAction("Index");
        }
    }
}
