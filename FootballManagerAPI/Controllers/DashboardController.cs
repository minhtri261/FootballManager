using FootballManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballManagerAPI.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly FootballContext _context;
        private const int PlayerClubId = 1;

        public DashboardController(FootballContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var state = await _context.GameStates.FirstOrDefaultAsync();
            if (state == null) return NotFound(new { error = "GameState chưa được khởi tạo." });

            var club = await _context.Clubs
                .Include(c => c.Footballers)
                .Include(c => c.TournamentClubs)
                .FirstOrDefaultAsync(c => c.Id == PlayerClubId && !c.IsBot);

            if (club == null) return NotFound(new { error = "Không tìm thấy CLB người chơi." });

            // Tournaments trong mùa này mà club tham gia
            var myTournamentIds = club.TournamentClubs.Select(tc => tc.TournamentId).ToList();
            var tournaments = await _context.Tournaments
                .Where(t => t.SeasonNumber == state.CurrentSeason && myTournamentIds.Contains(t.Id))
                .ToListAsync();

            var tournamentDtos = new List<TournamentDashDto>();
            foreach (var t in tournaments)
            {
                var standings = await _context.TournamentClubs
                    .Include(tc => tc.Club)
                    .Where(tc => tc.TournamentId == t.Id)
                    .OrderByDescending(tc => tc.Points)
                    .ThenByDescending(tc => tc.GoalsFor - tc.GoalsAgainst)
                    .ThenByDescending(tc => tc.GoalsFor)
                    .ToListAsync();

                tournamentDtos.Add(new TournamentDashDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Type = t.Type.ToString(),
                    Standings = standings.Select((tc, i) => new StandingDashDto
                    {
                        Rank = i + 1,
                        ClubId = tc.ClubId,
                        ClubName = tc.Club?.Name ?? "",
                        Played = tc.Played,
                        Won = tc.Won,
                        Drawn = tc.Drawn,
                        Lost = tc.Lost,
                        GoalsFor = tc.GoalsFor,
                        GoalsAgainst = tc.GoalsAgainst,
                        Points = tc.Points,
                        IsMyClub = tc.ClubId == PlayerClubId
                    }).ToList()
                });
            }

            // Tất cả trận đấu của CLB trong mùa này
            var allMatches = await _context.Matches
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Include(m => m.Tournament)
                .Where(m => m.SeasonNumber == state.CurrentSeason &&
                           (m.HomeClubId == PlayerClubId || m.AwayClubId == PlayerClubId))
                .OrderBy(m => m.Round)
                .ToListAsync();

            var nextMatch = allMatches.FirstOrDefault(m => !m.IsPlayed);
            MatchDashDto? nextMatchDto = null;
            if (nextMatch != null)
            {
                nextMatchDto = new MatchDashDto
                {
                    MatchId = nextMatch.Id,
                    TournamentId = nextMatch.TournamentId,
                    TournamentName = nextMatch.Tournament?.Name ?? "",
                    Round = nextMatch.Round,
                    HomeClubId = nextMatch.HomeClubId,
                    HomeClubName = nextMatch.HomeClub?.Name ?? "",
                    AwayClubId = nextMatch.AwayClubId ?? 0,
                    AwayClubName = nextMatch.AwayClub?.Name ?? "",
                    IsHome = nextMatch.HomeClubId == PlayerClubId,
                    IsPlayed = false
                };
            }

            var finishedMatches = allMatches
                .Where(m => m.IsPlayed)
                .OrderByDescending(m => m.Round)
                .Take(15)
                .Select(m => new MatchDashDto
                {
                    MatchId = m.Id,
                    TournamentId = m.TournamentId,
                    TournamentName = m.Tournament?.Name ?? "",
                    Round = m.Round,
                    HomeClubId = m.HomeClubId,
                    HomeClubName = m.HomeClub?.Name ?? "",
                    AwayClubId = m.AwayClubId ?? 0,
                    AwayClubName = m.AwayClub?.Name ?? "",
                    HomeGoals = m.HomeGoals,
                    AwayGoals = m.AwayGoals,
                    Result = m.Result.ToString(),
                    IsHome = m.HomeClubId == PlayerClubId,
                    IsPlayed = true
                })
                .ToList();

            return Ok(new DashboardDto
            {
                GameState = new GameStateDashDto
                {
                    CurrentSeason = state.CurrentSeason,
                    CurrentWeek = state.CurrentWeek,
                    CurrentPhase = (int)state.CurrentPhase,
                    PhaseName = state.CurrentPhase.ToString()
                },
                Club = new ClubDashDto
                {
                    Id = club.Id,
                    Name = club.Name,
                    Nation = club.Nation,
                    Money = club.Money,
                    LeagueCups = club.LeagueCups,
                    ChampionsCups = club.ChampionsCups,
                    NationalCups = club.NationalCups,
                    TrainingQuality = club.TrainingQuality
                },
                Players = club.Footballers
                    .OrderBy(f => f.Position)
                    .Select(f => new PlayerDashDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Age = f.Age,
                        Nation = f.Nation,
                        Position = f.Position.ToString(),
                        Quality = f.Quality,
                        Potential = f.Potential,
                        ContractYears = f.ContractYears,
                        Status = f.Status.ToString(),
                        IsTransferListed = f.IsTransferListed
                    }).ToList(),
                Tournaments = tournamentDtos,
                NextMatch = nextMatchDto,
                FinishedMatches = finishedMatches
            });
        }
    }

    public class DashboardDto
    {
        public GameStateDashDto GameState { get; set; } = new();
        public ClubDashDto Club { get; set; } = new();
        public List<PlayerDashDto> Players { get; set; } = new();
        public List<TournamentDashDto> Tournaments { get; set; } = new();
        public MatchDashDto? NextMatch { get; set; }
        public List<MatchDashDto> FinishedMatches { get; set; } = new();
    }

    public class GameStateDashDto
    {
        public int CurrentSeason { get; set; }
        public int CurrentWeek { get; set; }
        public int CurrentPhase { get; set; }
        public string PhaseName { get; set; } = "";
    }

    public class ClubDashDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Nation { get; set; } = "";
        public decimal Money { get; set; }
        public int LeagueCups { get; set; }
        public int ChampionsCups { get; set; }
        public int NationalCups { get; set; }
        public int TrainingQuality { get; set; }
    }

    public class PlayerDashDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Nation { get; set; } = "";
        public string Position { get; set; } = "";
        public int Quality { get; set; }
        public int Potential { get; set; }
        public int ContractYears { get; set; }
        public string Status { get; set; } = "";
        public bool IsTransferListed { get; set; }
    }

    public class TournamentDashDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public List<StandingDashDto> Standings { get; set; } = new();
    }

    public class StandingDashDto
    {
        public int Rank { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = "";
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public bool IsMyClub { get; set; }
    }

    public class MatchDashDto
    {
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = "";
        public int Round { get; set; }
        public int HomeClubId { get; set; }
        public string HomeClubName { get; set; } = "";
        public int AwayClubId { get; set; }
        public string AwayClubName { get; set; } = "";
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public string? Result { get; set; }
        public bool IsHome { get; set; }
        public bool IsPlayed { get; set; }
    }
}
