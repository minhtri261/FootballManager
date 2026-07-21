using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace FootballManager.Data.Repositories
{
    public class ClubRepository : BaseRepository<Club>, IClubRepository
    {
        public ClubRepository(FootballContext context) : base(context) { }

        // Lấy tất cả Club là Bot cùng với danh sách Footballers của họ
        public async Task<List<Club>> GetBotClubsWithPlayersAsync()
        {
            return await _dbSet
                .Where(c => c.IsBot)
                .Include(c => c.Footballers)
                .ToListAsync();
        }

        // Lấy tất cả Club cùng quốc gia kèm danh sách Footballers
        public async Task<List<Club>> GetClubsByNationAsync(string nation)
        {
            return await _dbSet
                .Where(c => c.Nation == nation)
                .Include(c => c.Footballers)
                .ToListAsync();
        }

        // Lấy 1 Club theo Id kèm danh sách Footballers
        public async Task<Club?> GetByIdWithPlayersAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Footballers)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}