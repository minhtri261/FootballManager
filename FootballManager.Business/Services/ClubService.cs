using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using System.Numerics;

namespace FootballManager.Business.Services
{
    public class ClubService : BaseService<Club>, IClubService
    {
        private readonly IClubRepository _clubRepo;
        private readonly GameSettings _settings;
        public ClubService(IClubRepository repo, IOptions<GameSettings> settings) : base(repo)
        {
            _clubRepo = repo;
            _settings = settings.Value;
        }

        public async Task<Club?> GetWithPlayersAsync() => await _clubRepo.GetWithPlayersAsync(_settings.MyClubId);

        public async Task<IEnumerable<Footballer>> GetMyPlayersAsync()
        {
            return await _clubRepo.GetPlayersByClubIdAsync(_settings.MyClubId);
        }

        public async Task<Footballer?> GetPlayerDetailAsync(int playerId)
        {
            var player = await _clubRepo.GetPlayerByIdAsync(playerId);
            if (player?.ClubId != _settings.MyClubId)
                return null; // chỉ xem được cầu thủ của đội mình

            return player;
        }
    }
}
