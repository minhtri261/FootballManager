using FootballManagerMVC.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerMVC.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApiClient _api;

        public DashboardController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _api.GetAsync<DashboardViewModel>("dashboard");
            return View(data ?? new DashboardViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> NextWeek()
        {
            await _api.PostAsync("game/next-week");
            return RedirectToAction(nameof(Index));
        }
    }
}
