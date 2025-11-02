using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IFootballerService : IBaseService<Footballer>
    {
        Task<IEnumerable<Footballer>> GetByClubAsync(int clubId);
    }
}
