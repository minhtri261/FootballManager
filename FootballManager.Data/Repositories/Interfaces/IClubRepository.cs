using FootballManager.Data.Entities;
using System.Numerics;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IClubRepository : IGenericRepository<Club>
    {
        Task<Club?> GetWithPlayersAsync(int id);
        Task<IEnumerable<Footballer>> GetPlayersByClubIdAsync(int clubId);
        Task<Footballer?> GetPlayerByIdAsync(int id);

        Task<int> CountByClubAsync(int clubId);

        Task SetClubFinalizedAsync(int clubId, bool isFinalized);

        Task<int> CountByPositionAsync(int clubId, string position);
    }
}
