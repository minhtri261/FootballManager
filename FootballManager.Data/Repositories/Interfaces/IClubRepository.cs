using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IClubRepository : IGenericRepository<Club>
    {
        Task<Club?> GetWithPlayersAsync(int id);
    }
}
