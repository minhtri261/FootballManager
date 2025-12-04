using FootballManager.Business.Services.Interfaces;
using FootballManager.Data;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class BotLineupService : BaseService<MatchLineup> , IBotLineupService
    {
        private readonly IMatchLineupRepository _lineupRepo;
        private readonly IMatchStatsRepository _statsRepo;
        private readonly IFootballerRepository _footballerRepo;
        private readonly Random _rng = new Random();

        public BotLineupService(
            IMatchLineupRepository lineupRepo,
            IMatchStatsRepository statsRepo,
            IFootballerRepository footballerRepo
        ) : base(lineupRepo)
        {
            _lineupRepo = lineupRepo;
            _statsRepo = statsRepo;
            _footballerRepo = footballerRepo;
        }
        public async Task<Dictionary<string, int>> ChooseFormationAsync(int tournamentId, int botClubId, int opponentClubId, int matchId)
        {
            var opponentPlayers = await _lineupRepo.GetLineupPlayersAsync(tournamentId, matchId, opponentClubId);
            var botPlayers = await _lineupRepo.GetLineupPlayersAsync(tournamentId, matchId, botClubId);

            // fallback nếu chưa có lineup
            if (!opponentPlayers.Any())
            {
                var oppFootballers = await _footballerRepo.GetByClubAsync(opponentClubId);
                opponentPlayers = oppFootballers
                    .Select(f => new MatchLineupPlayer { Footballer = f })
                    .ToList();
            }

            if (!botPlayers.Any())
            {
                var botFootballers = await _footballerRepo.GetByClubAsync(botClubId);
                botPlayers = botFootballers
                    .Select(f => new MatchLineupPlayer { Footballer = f })
                    .ToList();
            }

            int opponentGoalsRank = await _statsRepo.GetGoalsRankAsync(tournamentId, opponentClubId);
            int opponentConcededRank = await _statsRepo.GetConcededRankAsync(tournamentId, opponentClubId);

            var topScorers = await _statsRepo.GetTopScorersAsync(tournamentId);
            var topScorerIds = new HashSet<int>(topScorers.Select(f => f.Id));

            int opponentRank = await _statsRepo.GetCurrentRankAsync(tournamentId, opponentClubId);
            int botRank = await _statsRepo.GetCurrentRankAsync(tournamentId, botClubId);

            int opponentCFQuality = opponentPlayers.Where(p => p.Footballer.Position == "CF").Sum(p => p.Footballer.Quality);
            int opponentMFQuality = opponentPlayers.Where(p => p.Footballer.Position is "DM" or "CM" or "AM").Sum(p => p.Footballer.Quality);
            int opponentCBQuality = opponentPlayers.Where(p => p.Footballer.Position == "CB").Sum(p => p.Footballer.Quality);
            double opponentAvgQuality = opponentPlayers.Any() ? opponentPlayers.Average(p => p.Footballer.Quality) : 0;

            int botMFQuality = botPlayers.Where(p => p.Footballer.Position is "DM" or "CM" or "AM").Sum(p => p.Footballer.Quality);
            double botAvgQuality = botPlayers.Any() ? botPlayers.Average(p => p.Footballer.Quality) : 0;

            var score = new Dictionary<string, int>
            {
                ["3-2-1"] = 0,
                ["2-3-1"] = 0,
                ["2-2-2"] = 0,
                ["1-3-2"] = 0
            };

            if (opponentCFQuality > 18) score["3-2-1"]++;
            if (opponentGoalsRank <= 3) score["3-2-1"]++;
            if (opponentPlayers.Any(p => topScorerIds.Contains(p.Footballer.Id))) score["3-2-1"]++;

            if (Math.Abs(opponentAvgQuality - botAvgQuality) is >= 1 and <= 2) score["2-3-1"]++;
            if (opponentMFQuality < botMFQuality) score["2-3-1"]++;
            if (botRank < opponentRank) score["2-3-1"]++;
            if (opponentGoalsRank <= 4) score["2-3-1"]++;

            if (opponentConcededRank <= 5) score["2-2-2"]++;
            if (botAvgQuality - opponentAvgQuality > 2) score["2-2-2"]++;
            if (opponentCBQuality < 20) score["2-2-2"]++;

            return score;
        }

        // CreateBotLineupAsync thử từng formation theo thứ tự điểm giảm dần
        public async Task<string> CreateBotLineupAsync(
            int tournamentId,
            int botClubId,
            int opponentClubId,
            int matchId
        )
        {
            var formationScores = await ChooseFormationAsync(tournamentId, botClubId, opponentClubId, matchId);

            // Lấy danh sách cầu thủ CLB
            var allPlayers = await _footballerRepo.GetByClubAsync(botClubId);

            // Gom theo position, sort giảm dần theo chất lượng
            var grouped = allPlayers
                .GroupBy(p => p.Position)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Quality).ToList());

            // thử từng formation theo điểm giảm dần
            foreach (var formation in formationScores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key))
            {
                var req = ParseFormation(formation);

                if (!HasEnoughPlayers(req, grouped))
                    continue; // không đủ -> thử formation tiếp theo

                // Build lineup
                var selected = SelectPlayers(req, grouped);

                var lineup = new MatchLineup
                {
                    MatchId = matchId,
                    ClubId = botClubId,
                    Formation = formation,
                    Players = selected.Select(p => new MatchLineupPlayer
                    {
                        FootballerId = p.Id
                    }).ToList()
                };

                await _lineupRepo.AddAsync(lineup);
                await _lineupRepo.SaveChangesAsync();

                return formation; // ✅ formation đã dùng
            }

            return "NO_VALID_FORMATION";
        }


        private Dictionary<string, int> ParseFormation(string formation)
        {
            var parts = formation.Split('-').Select(int.Parse).ToArray();

            return new Dictionary<string, int>
            {
                ["GK"] = 1,
                ["CB"] = parts[0],
                ["MF"] = parts[1], // DM+CM+AM
                ["CF"] = parts[2]
            };
        }

        private bool HasEnoughPlayers(Dictionary<string, int> req,
                              Dictionary<string, List<Footballer>> grouped)
        {
            int gk = grouped.ContainsKey("GK") ? grouped["GK"].Count : 0;
            int cb = grouped.ContainsKey("CB") ? grouped["CB"].Count : 0;
            int cf = grouped.ContainsKey("CF") ? grouped["CF"].Count : 0;

            int mf = 0;
            if (grouped.ContainsKey("DM")) mf += grouped["DM"].Count;
            if (grouped.ContainsKey("CM")) mf += grouped["CM"].Count;
            if (grouped.ContainsKey("AM")) mf += grouped["AM"].Count;

            return gk >= req["GK"]
                && cb >= req["CB"]
                && mf >= req["MF"]
                && cf >= req["CF"];
        }

        private List<Footballer> SelectPlayers(
    Dictionary<string, int> req,
    Dictionary<string, List<Footballer>> grouped)
        {
            var result = new List<Footballer>();

            result.AddRange(grouped["GK"].Take(req["GK"]));
            result.AddRange(grouped["CB"].Take(req["CB"]));
            result.AddRange(
                (grouped.GetValueOrDefault("DM") ?? new List<Footballer>())
                .Concat(grouped.GetValueOrDefault("CM") ?? new List<Footballer>())
                .Concat(grouped.GetValueOrDefault("AM") ?? new List<Footballer>())
                .OrderByDescending(x => x.Quality)
                .Take(req["MF"])
            );
            result.AddRange(grouped["CF"].Take(req["CF"]));

            return result;
        }


    }
}
