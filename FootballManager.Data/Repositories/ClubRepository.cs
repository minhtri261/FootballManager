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
    }
}