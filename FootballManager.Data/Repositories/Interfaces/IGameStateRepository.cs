using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IGameStateRepository : IGenericRepository<GameState>
    {
        Task<GameState> GetSingletonAsync();
    }
}
