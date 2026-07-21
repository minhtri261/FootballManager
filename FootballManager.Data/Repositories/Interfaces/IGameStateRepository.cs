using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IGameStateRepository : IBaseRepository<GameState>
    {
        Task<GameState?> GetCurrentStateAsync();
        Task<ScheduleTemplate?> GetTemplateByWeekAsync(int week);
        Task<List<ScheduleTemplate>> GetAllTemplatesAsync();
    }
}
