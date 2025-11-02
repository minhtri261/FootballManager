using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IClubService : IBaseService<Club>
    {
        Task<Club?> GetWithPlayersAsync(int id);
    }
}
