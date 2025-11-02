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
    }
}
