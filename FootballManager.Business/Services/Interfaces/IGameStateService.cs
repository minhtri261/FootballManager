using FootballManager.Business.DTOs;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IGameStateService
    {
        Task<GameStepResultDto> AdvanceNextWeekAsync();
    }
}
