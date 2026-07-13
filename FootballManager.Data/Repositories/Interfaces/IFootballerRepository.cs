using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IFootballerRepository : IBaseRepository<Footballer>
    {
        Task<IEnumerable<Footballer>> GetByClubAsync(int clubId);
        Task<int> CountByClubAsync(int clubId);
    }
}
