using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class TournamentService : BaseService<Tournament>, ITournamentService
    {
        private readonly ITournamentRepository _repo;
        public TournamentService(ITournamentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<Tournament?> GetWithClubsAsync(int id) => await _repo.GetWithClubsAsync(id);
    }
}
