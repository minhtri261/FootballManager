using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/footballers")]
public class FootballerController : ControllerBase
{
    private readonly IFootballerService _service;

    public FootballerController(IFootballerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
    string? keyword = null,
    string? sortBy = "Name",
    bool desc = false,
    int? clubId = null,
    int pageNumber = 1,
    int pageSize = 20)
    {
        var list = await _service.GetAllAsync();

        // 🔍 Lọc theo từ khóa
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.ToLower();
            list = list.Where(f =>
                f.Name.ToLower().Contains(keyword) ||
                f.Nation.ToLower().Contains(keyword) ||
                (f.Club != null && f.Club.Name.ToLower().Contains(keyword))
            );
        }

        // 🔍 Lọc theo CLB
        if (clubId.HasValue)
            list = list.Where(f => f.ClubId == clubId.Value);

        // 📊 Sắp xếp
        list = sortBy?.ToLower() switch
        {
            "age" => desc ? list.OrderByDescending(f => f.Age) : list.OrderBy(f => f.Age),
            "quality" => desc ? list.OrderByDescending(f => f.Quality) : list.OrderBy(f => f.Quality),
            "nation" => desc ? list.OrderByDescending(f => f.Nation) : list.OrderBy(f => f.Nation),
            "club" => desc ? list.OrderByDescending(f => f.Club!.Name) : list.OrderBy(f => f.Club!.Name),
            _ => desc ? list.OrderByDescending(f => f.Name) : list.OrderBy(f => f.Name)
        };

        // 📄 Phân trang
        var totalItems = list.Count();
        var pagedList = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map sang DTO
        var result = pagedList.Select(f => new FootballerDto
        {
            Id = f.Id,
            Name = f.Name,
            Age = f.Age,
            Nation = f.Nation,
            Position = f.Position,
            Quality = f.Quality,
            ClubId = f.ClubId,
            ClubName = f.Club?.Name,
            ContractYears = f.ContractYears,
            Status = f.Status,
            AwardQBV = f.AwardQBV,
            AwardQBB = f.AwardQBB,
            AwardQBD = f.AwardQBD,
            IsTransferListed = f.IsTransferListed
        });

        // Trả thêm thông tin tổng số trang
        return Ok(new
        {
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            Items = result
        });
    }



    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _service.GetByIdAsync(id);
        var result = item == null ? null : new FootballerDto
        {
            Id = item.Id,
            Name = item.Name,
            Age = item.Age,
            Nation = item.Nation,
            Position = item.Position,
            Quality = item.Quality,
            ClubId = item.ClubId,
            ClubName = item.Club?.Name,
            ContractYears = item.ContractYears,
            Status = item.Status,
            AwardQBV = item.AwardQBV,
            AwardQBB = item.AwardQBB,
            AwardQBD = item.AwardQBD,
            IsTransferListed = item.IsTransferListed
        };
        return item == null ? NotFound() : Ok(result);
    }

    [HttpGet("by-club/{clubId}")]
    public async Task<IActionResult> GetByClub(int clubId) => Ok(await _service.GetByClubAsync(clubId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FootballerDto dto)
    {
        var f = new Footballer
        {
            Name = dto.Name,
            Age = dto.Age,
            Nation = dto.Nation,
            Position = dto.Position,
            Quality = dto.Quality,
            ClubId = dto.ClubId,
            ContractYears = dto.ContractYears,
            Status = dto.Status,
            AwardQBV = dto.AwardQBV,
            AwardQBB = dto.AwardQBB,
            AwardQBD = dto.AwardQBD,
            IsTransferListed = dto.IsTransferListed
        };
        await _service.AddAsync(f);
        return CreatedAtAction(nameof(Get), new { id = f.Id }, f);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] FootballerDto dto)
    {
        var f = await _service.GetByIdAsync(id);
        if (f == null) return NotFound();

        // ✅ Cập nhật field được phép chỉnh sửa
        f.Name = dto.Name;
        f.Age = dto.Age;
        f.Nation = dto.Nation;
        f.Position = dto.Position;
        f.Quality = dto.Quality;
        f.ClubId = dto.ClubId;
        f.ContractYears = dto.ContractYears;
        f.Status = dto.Status;
        f.AwardQBV = dto.AwardQBV;
        f.AwardQBB = dto.AwardQBB;
        f.AwardQBD = dto.AwardQBD;
        f.IsTransferListed = dto.IsTransferListed;

        await _service.UpdateAsync(f);
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
