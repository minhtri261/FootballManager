using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class GameStateRepository : BaseRepository<GameState>, IGameStateRepository
    {
        public GameStateRepository(FootballContext context) : base(context) { }

        public async Task<GameState?> GetCurrentStateAsync()
        => await _dbSet.FirstOrDefaultAsync();

        public async Task<ScheduleTemplate?> GetTemplateByWeekAsync(int week)
            => await _context.ScheduleTemplates.FirstOrDefaultAsync(t => t.Week == week);
    }
}
