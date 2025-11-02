using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface ITournamentService : IBaseService<Tournament>
    {
        Task<Tournament?> GetWithClubsAsync(int id);
    }
}
