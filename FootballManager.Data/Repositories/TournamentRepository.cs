using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class TournamentRepository : BaseRepository<Tournament>, ITournamentRepository
    {
        public TournamentRepository(FootballContext context) : base(context) { }

        public async Task<Tournament?> GetWithClubsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.TournamentClubs)
                    .ThenInclude(tc => tc.Club)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public override async Task<Tournament?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(t => t.TournamentClubs)
                    .ThenInclude(tc => tc.Club)  // ✅ load cả thông tin CLB
                .Include(t => t.Matches)       // ✅ load danh sách trận (nếu có)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
