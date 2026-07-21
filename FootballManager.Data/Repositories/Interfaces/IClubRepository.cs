using FootballManager.Data.Entities;
using System.Numerics;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IClubRepository : IBaseRepository<Club>
    {
        // Lấy tất cả Club là Bot cùng với danh sách Footballers của họ
        Task<List<Club>> GetBotClubsWithPlayersAsync();

        // Lấy tất cả Club cùng quốc gia kèm danh sách Footballers
        Task<List<Club>> GetClubsByNationAsync(string nation);

        // Lấy 1 Club theo Id kèm danh sách Footballers
        Task<Club?> GetByIdWithPlayersAsync(int id);
    }
}
