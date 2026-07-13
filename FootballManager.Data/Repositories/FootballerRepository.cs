using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class FootballerRepository : BaseRepository<Footballer>, IFootballerRepository
    {
        public FootballerRepository(FootballContext context) : base(context) { }

        public async Task<IEnumerable<Footballer>> GetByClubAsync(int clubId)
        {
            return await _dbSet.Where(f => f.ClubId == clubId).ToListAsync();
        }

        public async Task<int> CountByClubAsync(int clubId)
        {
            return await _dbSet.CountAsync(f => f.ClubId == clubId);
        }
    }
}
