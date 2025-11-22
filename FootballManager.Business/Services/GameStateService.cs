using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class GameStateService : IGameStateService
    {
        private readonly IGameStateRepository _repo;
        private readonly ITransferService _transferService;

        public GameStateService(IGameStateRepository repo, ITransferService transferService)
        {
            _repo = repo;
            _transferService = transferService;
        }

        public async Task<GameState> GetStateAsync()
        {
            return await _repo.GetSingletonAsync();
        }

        public async Task<GameState> NextPhaseAsync()
        {
            var state = await _repo.GetSingletonAsync();

            switch (state.CurrentPhase)
            {
                case GamePhase.PreSeason:
                    state.CurrentPhase = GamePhase.TransferWindow;
                    break;
                case GamePhase.TransferWindow:
                    await _transferService.BotDecideWhoToBuyOrRenewAsync();
                    await _transferService.BotDecideTransfersAsync();
                    state.CurrentPhase = GamePhase.InSeason;
                    break;
                case GamePhase.InSeason:
                    state.CurrentPhase = GamePhase.SeasonSummary;
                    break;
                case GamePhase.SeasonSummary:
                    state.CurrentPhase = GamePhase.PreSeason;
                    state.CurrentSeason++;
                    break;
            }

            _repo.Update(state);
            await _repo.SaveChangesAsync();
            return state;
        }
    }
}
