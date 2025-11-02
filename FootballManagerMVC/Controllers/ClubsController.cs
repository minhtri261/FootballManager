using FootballManager.Web.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerMVC.Controllers
{
    public class ClubsController : Controller
    {
        private readonly ApiClient _api;

        public ClubsController(ApiClient api)
        {
            _api = api;
        }
        public async Task<IActionResult> Index()
        {
            var clubs = await _api.GetListAsync<Club>("clubs");
            return View(clubs);
        }

        public async Task<IActionResult> ClubDetail(int id)
        {
            var club = await _api.GetAsync<Club>($"clubs/{id}");
            return club == null ? NotFound() : View(club);
        }

        public IActionResult AdminCreate() => View();

        [HttpPost]
        public async Task<IActionResult> AdminCreate(Club club)
        {
            if (!ModelState.IsValid) return View(club);
            await _api.PostAsync("clubs", club);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AdminEdit(int id)
        {
            var club = await _api.GetAsync<Club>($"clubs/{id}");
            return club == null ? NotFound() : View(club);
        }

        [HttpPost]
        public async Task<IActionResult> AdminEdit(int id, Club club)
        {
            if (id != club.Id) return BadRequest();
            await _api.PutAsync($"clubs/{id}", club);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AdminDelete(int id)
        {
            var club = await _api.GetAsync<Club>($"clubs/{id}");
            return club == null ? NotFound() : View(club);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _api.DeleteAsync($"clubs/{id}");
            return RedirectToAction(nameof(Index));
        }
    }
}
    