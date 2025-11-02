using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/admin/tournaments")]
public class TournamentController : ControllerBase
{
    private readonly ITournamentService _service;
    public TournamentController(ITournamentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tournaments = await _service.GetAllAsync();

        var result = tournaments.Select(t => new TournamentDTO
        {
            Id = t.Id,
            Name = t.Name,
            SeasonNumber = t.SeasonNumber,
            TeamsCount = t.TeamsCount,
            PlayersPerMatch = t.PlayersPerMatch,
            Type = t.Type.ToString(),
            RewardByRank = string.IsNullOrEmpty(t.RewardByRank)
                ? new List<RewardRankDTO>()
                : JsonSerializer.Deserialize<List<RewardRankDTO>>(t.RewardByRank)!
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _service.GetByIdAsync(id);
        if (t == null) return NotFound();

        var dto = new TournamentDTO
        {
            Id = t.Id,
            Name = t.Name,
            SeasonNumber = t.SeasonNumber,
            TeamsCount = t.TeamsCount,
            PlayersPerMatch = t.PlayersPerMatch,
            Type = t.Type.ToString(),
            RewardByRank = string.IsNullOrEmpty(t.RewardByRank)
                ? new List<RewardRankDTO>()
                : JsonSerializer.Deserialize<List<RewardRankDTO>>(t.RewardByRank)!
        };

        return Ok(dto);
    }

    [HttpGet("{id}/clubs")]
    public async Task<IActionResult> GetWithClubs(int id)
    {
        var t = await _service.GetWithClubsAsync(id);
        if (t == null) return NotFound();

        var dto = new TournamentWithClubsDTO
        {
            Id = t.Id,
            Name = t.Name,
            SeasonNumber = t.SeasonNumber,
            TeamsCount = t.TeamsCount,
            PlayersPerMatch = t.PlayersPerMatch,
            Type = t.Type.ToString(),
            RewardByRank = string.IsNullOrEmpty(t.RewardByRank)
                ? new List<RewardRankDTO>()
                : JsonSerializer.Deserialize<List<RewardRankDTO>>(t.RewardByRank)!,
            Clubs = t.TournamentClubs.Select(tc => new TournamentClubDTO
            {
                ClubId = tc.ClubId,
                ClubName = tc.Club?.Name ?? "Unknown",
                Points = tc.Points,
                Rank = tc.Rank,
                GoalsFor = tc.GoalsFor,
                GoalsAgainst = tc.GoalsAgainst
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TournamentDTO dto)
    {
        var entity = new Tournament
        {
            Name = dto.Name,
            SeasonNumber = dto.SeasonNumber,
            TeamsCount = dto.TeamsCount,
            PlayersPerMatch = dto.PlayersPerMatch,
            Type = Enum.Parse<TournamentType>(dto.Type),
            RewardByRank = dto.RewardByRank == null
                ? null
                : JsonSerializer.Serialize(dto.RewardByRank)
        };

        await _service.AddAsync(entity);
        dto.Id = entity.Id;

        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TournamentDTO dto)
    {
        if (id != dto.Id) return BadRequest();

        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Name = dto.Name;
        existing.SeasonNumber = dto.SeasonNumber;
        existing.TeamsCount = dto.TeamsCount;
        existing.PlayersPerMatch = dto.PlayersPerMatch;
        existing.Type = Enum.Parse<TournamentType>(dto.Type);
        existing.RewardByRank = dto.RewardByRank == null
            ? null
            : JsonSerializer.Serialize(dto.RewardByRank);

        await _service.UpdateAsync(existing);
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
    
