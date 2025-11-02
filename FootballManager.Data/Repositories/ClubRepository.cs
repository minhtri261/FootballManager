using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class ClubRepository : BaseRepository<Club>, IClubRepository
    {
        public ClubRepository(FootballContext context) : base(context) { }

        public async Task<Club?> GetWithPlayersAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Footballers)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
