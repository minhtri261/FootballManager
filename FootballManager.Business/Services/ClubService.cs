using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class ClubService : BaseService<Club>, IClubService
    {
        private readonly IClubRepository _clubRepo;
        public ClubService(IClubRepository repo) : base(repo)
        {
            _clubRepo = repo;
        }

        public async Task<Club?> GetWithPlayersAsync(int id) => await _clubRepo.GetWithPlayersAsync(id);
    }
}
