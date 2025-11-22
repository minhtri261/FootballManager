using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IGameStateService
    {
        Task<GameState> GetStateAsync();
        Task<GameState> NextPhaseAsync();
    }
}
