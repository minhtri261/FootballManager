using FootballManager.Data.Entities;
using System.Numerics;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IClubService : IBaseService<Club>
    {
        Task<Club?> GetWithPlayersAsync();
        Task<IEnumerable<Footballer>> GetMyPlayersAsync();
        Task<Footballer?> GetPlayerDetailAsync(int playerId);
    }
}
