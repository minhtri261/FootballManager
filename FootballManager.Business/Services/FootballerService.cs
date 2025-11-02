using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class FootballerService : BaseService<Footballer>, IFootballerService
    {
        private readonly IFootballerRepository _repo;
        public FootballerService(IFootballerRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Footballer>> GetByClubAsync(int clubId) => await _repo.GetByClubAsync(clubId);
    }
}
