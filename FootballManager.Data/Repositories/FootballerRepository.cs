using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class FootballerRepository : BaseRepository<Footballer>, IFootballerRepository
    {
        public FootballerRepository(FootballContext context) : base(context) { }

        public override async Task<IEnumerable<Footballer>> GetAllAsync()
        {
            return await _dbSet
                .Include(f => f.Club)
                .ToListAsync();
        }

        public override async Task<Footballer?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(f => f.Club)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Footballer>> GetByClubAsync(int clubId)
        {
            return await _dbSet
                .Include(f => f.Club)
                .Where(f => f.ClubId == clubId)
                .ToListAsync();
        }
    }

}
