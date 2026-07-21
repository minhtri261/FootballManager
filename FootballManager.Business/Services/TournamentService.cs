using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IMatchRepository _matchRepo;

        public TournamentService(ITournamentRepository tournamentRepo, IMatchRepository matchRepo)
        {
            _tournamentRepo = tournamentRepo;
            _matchRepo = matchRepo;
        }

        public async Task<TournamentDetailDto?> GetTournamentDetailAsync(int tournamentId)
        {
            var tournament = await _tournamentRepo.GetByIdAsync(tournamentId);
            if (tournament == null) return null;

            var dto = new TournamentDetailDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                Type = tournament.Type,
                SeasonNumber = tournament.SeasonNumber
            };

            // BXH: League (toàn giải) và C1 (theo từng bảng) — Cup thuần knockout không có BXH
            if (tournament.Type == TournamentType.League)
            {
                var standings = await _tournamentRepo.GetStandingsAsync(tournamentId); // đã sort Points/GD giảm dần
                dto.Standings = standings.Select((tc, idx) => MapStanding(tc, idx + 1)).ToList();
            }
            else if (tournament.Type == TournamentType.C1)
            {
                var standings = await _tournamentRepo.GetStandingsAsync(tournamentId);
                foreach (var group in standings.GroupBy(tc => tc.Group ?? 0).OrderBy(g => g.Key))
                {
                    var ranked = group.ToList(); // GroupBy giữ nguyên thứ tự Points/GD tương đối trong từng bảng
                    for (int i = 0; i < ranked.Count; i++)
                        dto.Standings.Add(MapStanding(ranked[i], i + 1));
                }
            }

            // Rounds: luôn có — dùng làm lịch (League) hoặc bracket (Cup/C1 knockout)
            var matches = await _matchRepo.GetMatchesByTournamentAsync(tournamentId);
            dto.Rounds = matches
                .GroupBy(m => m.Round)
                .OrderBy(g => g.Key)
                .Select(g => new RoundDto
                {
                    Round = g.Key,
                    Matches = g.Select(m => new TournamentMatchDto
                    {
                        MatchId = m.Id,
                        Group = m.Group,
                        HomeClubId = m.HomeClubId,
                        HomeClubName = m.HomeClub?.Name ?? string.Empty,
                        AwayClubId = m.AwayClubId,
                        AwayClubName = m.AwayClub?.Name,
                        IsPlayed = m.IsPlayed,
                        HomeGoals = m.HomeGoals,
                        AwayGoals = m.AwayGoals,
                        Result = m.IsPlayed ? m.Result : null
                    }).ToList()
                }).ToList();

            return dto;
        }

        private static StandingDto MapStanding(TournamentClub tc, int rank) => new StandingDto
        {
            Rank = rank,
            Group = tc.Group,
            ClubId = tc.ClubId,
            ClubName = tc.Club?.Name ?? string.Empty,
            Played = tc.Played,
            Won = tc.Won,
            Drawn = tc.Drawn,
            Lost = tc.Lost,
            GoalsFor = tc.GoalsFor,
            GoalsAgainst = tc.GoalsAgainst,
            Points = tc.Points
        };
    }
}
