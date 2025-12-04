using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        public MatchRepository(FootballContext context) : base(context) { }
        public async Task AddRangeAsync(IEnumerable<Match> matches)
        {
            await _dbSet.AddRangeAsync(matches);
            await _context.SaveChangesAsync();
        }

        //Xem tất cả trận đấu thuộc giải đấu
        public async Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId)
        {
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId)
                .ToListAsync();
        }

        //Xem tất cả trận đấu thuộc giải đấu nhưng chưa đá
        public async Task<List<Match>> GetUnplayedMatchesByTournamentAsync(int tournamentId)
        {
            var query = _dbSet
                .Where(m => m.TournamentId == tournamentId && !m.IsPlayed);

            int? currentRound = await query
                .OrderBy(m => m.Round)
                .Select(m => (int?)m.Round)
                .FirstOrDefaultAsync();

            if (currentRound == null)
                return new List<Match>();

            return await _dbSet
                .Where(m =>
                    m.TournamentId == tournamentId &&
                    !m.IsPlayed &&
                    m.Round == currentRound)
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .ToListAsync();
        }

        //Lấy phong độ 3 trận gần nhất của đội bóng trong giải đấu
        public async Task<List<Match>> GetLastFiveMatchesAsync(int tournamentId, int clubId)
        {
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId &&
                            (m.HomeClubId == clubId || m.AwayClubId == clubId) &&
                            m.IsPlayed)
                .OrderByDescending(m => m.Id) // Giả sử Id tăng dần theo thời gian
                .Take(3)
                .ToListAsync();
        }

        //Kết quả tất cả các trận đấu của Round vừa đá xong
        public async Task<List<Match>> GetLastRoundResultsAsync(int tournamentId)
        {
            // Lấy round cao nhất đã đá
            int? lastRound = await _dbSet
                .Where(m => m.TournamentId == tournamentId && m.IsPlayed)
                .MaxAsync(m => (int?)m.Round);

            if (lastRound == null)
                return new List<Match>();

            return await _dbSet
                .Where(m => m.TournamentId == tournamentId && m.IsPlayed && m.Round == lastRound)
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .ToListAsync();

        }

        //Xem kết quả 1 đội cụ thể của Round vừa đá xong
        public async Task<List<Match>> GetLastRoundResultsForClubAsync(int tournamentId, int clubId)
        {
            // Lấy round cao nhất đã đá
            int? lastRound = await _dbSet
                .Where(m => m.TournamentId == tournamentId && m.IsPlayed)
                .MaxAsync(m => (int?)m.Round);
            if (lastRound == null)
                return new List<Match>();
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId &&
                            m.IsPlayed &&
                            m.Round == lastRound &&
                            (m.HomeClubId == clubId || m.AwayClubId == clubId))
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .ToListAsync();
        }
    }
}
