using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class GameStateRepository : BaseRepository<GameState>, IGameStateRepository
    {
        public GameStateRepository(FootballContext context) : base(context) { }

        public async Task<GameState> GetSingletonAsync()
        {
            var state = await _dbSet.FirstOrDefaultAsync();
            if (state == null)
            {
                state = new GameState
                {
                    CurrentSeason = 1,
                    CurrentPhase = GamePhase.PreSeason
                };
                await _dbSet.AddAsync(state);
                await _context.SaveChangesAsync();
            }
            return state;
        }
    }
}
