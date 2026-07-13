using FootballManager.Data.Entities;
using System.Numerics;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IClubRepository : IBaseRepository<Club>
    {
        // Lấy tất cả Club là Bot cùng với danh sách Footballers của họ
        Task<List<Club>> GetBotClubsWithPlayersAsync();
    }
}
