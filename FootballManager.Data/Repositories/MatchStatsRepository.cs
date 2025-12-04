using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Repositories
{
    public class MatchStatsRepository : IMatchStatsRepository
    {
        private readonly FootballContext _context;

        public MatchStatsRepository(FootballContext context)
        {
            _context = context;
        }

        // tổng bàn (không tính phản lưới)
        public async Task<int> GetTotalGoalsAsync(int tournamentId, int clubId)
        {
            var totalGoals = await _context.MatchGoals
            .Where(g => g.Match.TournamentId == tournamentId && g.ClubId == clubId && !g.IsOwnGoal)
            .CountAsync();
            return totalGoals;
        }

        // tất cả bàn mà club bị thủng lưới
        public async Task<int> GetTotalConcededGoalsAsync(int tournamentId, int clubId)
        {
            var totalConceded = await _context.MatchGoals
            .Where(g => g.Match.TournamentId == tournamentId &&
                        ((g.ClubId != clubId && !g.IsOwnGoal) || (g.ClubId == clubId && g.IsOwnGoal)))
            .CountAsync();

            return totalConceded;
        }

        //Thứ hạng hiện tại trong giải đấu
        public async Task<int> GetCurrentRankAsync(int tournamentId, int clubId)
        {
            var tClub = await _context.TournamentClubs
                .FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.ClubId == clubId);
            return tClub?.Rank ?? int.MaxValue;
        }

        public async Task<List<Footballer>> GetTopScorersAsync(int tournamentId, int top = 5)
        {
            var topScorers = await _context.MatchGoals
            .Where(g => g.Match.TournamentId == tournamentId && !g.IsOwnGoal)
            .GroupBy(g => g.FootballerId)
            .Select(gr => new { FootballerId = gr.Key, Goals = gr.Count() })
            .OrderByDescending(x => x.Goals)
            .Take(top)
            .Join(_context.Footballers,
                  x => x.FootballerId,
                  f => f.Id,
                  (x, f) => f)
            .ToListAsync();

            return topScorers;
        }

        public async Task<int> GetGoalsRankAsync(int tournamentId, int clubId)
        {
            var grouped = await _context.MatchGoals
                .Where(g => g.Match.TournamentId == tournamentId && !g.IsOwnGoal)
                .GroupBy(g => g.ClubId)
                .Select(gr => new { ClubId = gr.Key, Goals = gr.Count() })
                .OrderByDescending(x => x.Goals)
                .ToListAsync();

            var ranks = grouped.Select((x, idx) => new { x.ClubId, Rank = idx + 1 }).ToList();
            var rec = ranks.FirstOrDefault(r => r.ClubId == clubId);
            return rec?.Rank ?? int.MaxValue;
        }

        public async Task<int> GetConcededRankAsync(int tournamentId, int clubId)
        {
            var concededPerClub = await _context.MatchGoals
            .Where(g => g.Match.TournamentId == tournamentId)
            .Select(g => new
            {
                MatchId = g.MatchId,
                GoalClubId = g.ClubId,
                IsOwnGoal = g.IsOwnGoal
            })
            .ToListAsync();

            var clubIdsInTournament = await _context.TournamentClubs
            .Where(tc => tc.TournamentId == tournamentId)
            .Select(tc => tc.ClubId)
            .ToListAsync();

            var concededDict = clubIdsInTournament.ToDictionary(id => id, id => 0);

            foreach (var g in concededPerClub)
            {
                if (g.IsOwnGoal)
                {
                    if (concededDict.ContainsKey(g.GoalClubId))
                        concededDict[g.GoalClubId] += 1;
                }
                else
                {
                }
            }

            var matches = await _context.Matches
            .Where(m => m.TournamentId == tournamentId)
            .Select(m => new { m.Id, m.HomeClubId, m.AwayClubId })
            .ToListAsync();

            var conceded = new Dictionary<int, int>();
            foreach (var id in clubIdsInTournament) conceded[id] = 0;

            var goals = await _context.MatchGoals
                .Where(g => g.Match.TournamentId == tournamentId)
                .ToListAsync();

            foreach (var g in goals)
            {
                if (g.IsOwnGoal)
                {
                    // own goal: the club associated with goal concedes 1
                    if (conceded.ContainsKey(g.ClubId))
                        conceded[g.ClubId] += 1;
                }
                else
                {
                    // find match participants
                    var match = matches.FirstOrDefault(m => m.Id == g.MatchId);
                    if (match == null) continue;
                    // if g.ClubId == home => away conceded; if g.ClubId == away => home conceded
                    if (g.ClubId == match.HomeClubId && match.AwayClubId.HasValue)
                    {
                        if (conceded.ContainsKey(match.AwayClubId.Value))
                            conceded[match.AwayClubId.Value] += 1;
                    }
                    else if (g.ClubId == match.AwayClubId)
                    {
                        if (conceded.ContainsKey(match.HomeClubId))
                            conceded[match.HomeClubId] += 1;
                    }
                }
            }

            var ordered = conceded.OrderByDescending(kv => kv.Value).ToList();
            int rank = ordered.FindIndex(kv => kv.Key == clubId);
            return rank >= 0 ? rank + 1 : int.MaxValue;
        }
    }
}
