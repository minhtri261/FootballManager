using FootballManager.Web.Services;
using FootballManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerMVC.Controllers
{
    public class FootballersController : Controller
    {
        private readonly ApiClient _api;
        private const int PageSize = 20;

        public FootballersController(ApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(string? keyword, string? sortBy, bool desc = false, int? clubId = null, int pageNumber = 1)
        {
            // Gửi query đến API (API của bạn nên hỗ trợ các query params này)
            string endpoint = $"footballers?keyword={keyword}&sortBy={sortBy}&desc={desc}&clubId={clubId}&pageNumber={pageNumber}&pageSize={PageSize}";
            var response = await _api.GetAsync<PagedResponse<Footballer>>(endpoint);

            // Lấy danh sách CLB để filter
            ViewBag.Clubs = await _api.GetListAsync<Club>("clubs");

            // Gửi lại các tham số để hiển thị đúng khi user tìm kiếm/lọc
            ViewBag.Keyword = keyword;
            ViewBag.SortBy = sortBy;
            ViewBag.Desc = desc;
            ViewBag.ClubId = clubId;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = response.TotalPages;

            return View(response.Items);
        }

        public async Task<IActionResult> FootballerDetails(int id)
        {
            var f = await _api.GetAsync<Footballer>($"footballers/{id}");
            return f == null ? NotFound() : View(f);
        }

        public async Task<IActionResult> AdminCreate()
        {
            ViewBag.Clubs = await _api.GetListAsync<Club>("clubs");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminCreate(Footballer f)
        {
            if (!ModelState.IsValid) return View(f);
            if (f.ClubId == null || f.ClubId == 0)
            {
                f.ClubId = null;
                f.IsTransferListed = true;
            }
            await _api.PostAsync("footballers", f);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AdminEdit(int id)
        {
            ViewBag.Clubs = await _api.GetListAsync<Club>("clubs");
            var f = await _api.GetAsync<Footballer>($"footballers/{id}");
            return f == null ? NotFound() : View(f);
        }

        [HttpPost]
        public async Task<IActionResult> AdminEdit(int id, Footballer f)
        {
            if (id != f.Id) return BadRequest();
            if (f.ClubId == null || f.ClubId == 0)
            {
                f.ClubId = null;
                f.IsTransferListed = true;
            }
            await _api.PutAsync($"footballers/{id}", f);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AdminDelete(int id)
        {
            var f = await _api.GetAsync<Footballer>($"footballers/{id}");
            return f == null ? NotFound() : View(f);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _api.DeleteAsync($"footballers/{id}");
            return RedirectToAction(nameof(Index));
        }
    }
}
