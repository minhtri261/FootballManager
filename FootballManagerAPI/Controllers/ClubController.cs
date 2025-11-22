using FootballManager.Business.DTOs;
using FootballManager.Business.Services;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [ApiController]
    [Route("api/admin/clubs")]
    public class ClubController : ControllerBase
    {
        private readonly IClubService _service;
        public ClubController(IClubService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClubCreateDto dto)
        {
            var club = new Club
            {
                Name = dto.Name,
                Nation = dto.Nation,
                Money = dto.Money,
                LeagueCups = dto.LeagueCups,
                ChampionsCups = dto.ChampionsCups,
                NationalCups = dto.NationalCups,
                TrainingQuality = dto.TrainingQuality,
                IsBot = dto.IsBot
            };

            await _service.AddAsync(club);
            return CreatedAtAction(nameof(Get), new { id = club.Id }, club);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClubUpdateDto dto)
        {
            var club = await _service.GetByIdAsync(id);
            if (club == null) return NotFound();

            // ✅ Cập nhật field được phép chỉnh sửa
            club.Name = dto.Name;
            club.Nation = dto.Nation;
            club.Money = dto.Money;
            club.LeagueCups = dto.LeagueCups;
            club.ChampionsCups = dto.ChampionsCups;
            club.NationalCups = dto.NationalCups;
            club.TrainingQuality = dto.TrainingQuality;
            club.IsBot = dto.IsBot;

            await _service.UpdateAsync(club);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            await _service.DeleteAsync(item);
            return NoContent();
        }
    }
}
