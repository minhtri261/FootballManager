using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface ITournamentRepository : IGenericRepository<Tournament>
    {
        Task<Tournament?> GetWithClubsAsync(int id);
    }
}
