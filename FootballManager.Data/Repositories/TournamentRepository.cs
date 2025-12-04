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

        //Lấy tất cả các giải đấu theo SeasonNumber
        public async Task<List<Tournament>> GetBySeasonNumberAsync(int seasonNumber)
        {
            return await _dbSet
                .Where(t => t.SeasonNumber == seasonNumber)
                .Include(t => t.Matches)
                .ToListAsync();
        }

        //Lấy TournamentClub theo TournamentId và ClubId
        public async Task<TournamentClub?> GetTournamentClubAsync(int tournamentId, int clubId)
        {
            return await _context.TournamentClubs
                .FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.ClubId == clubId);
        }
    }
}
