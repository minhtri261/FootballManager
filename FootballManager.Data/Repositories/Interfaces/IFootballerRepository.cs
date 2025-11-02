using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IFootballerRepository : IGenericRepository<Footballer>
    {
        Task<IEnumerable<Footballer>> GetByClubAsync(int clubId);
    }
}
