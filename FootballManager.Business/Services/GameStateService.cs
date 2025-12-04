using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class GameStateService : IGameStateService
    {
        private readonly IGameStateRepository _repo;
        private readonly ITransferService _transferService;
        private readonly IMatchService _matchService;

        public GameStateService(IGameStateRepository repo, IMatchService matchService, ITransferService transferService)
        {
            _repo = repo;
            _transferService = transferService;
            _matchService = matchService;
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
                    //Bot sẽ kiểm tra lại lần cuối xem còn đội nào chưa gửi đề nghị chuyển nhượng hay gia hạn hợp đồng không
                    await _transferService.BotDecideWhoToBuyOrRenewAsync();

                    //Nếu có cầu thủ nào đang chờ quyết định thì bot sẽ chọn CLB cho họ nốt
                    await _transferService.BotDecideTransfersAsync();

                    // Chuyển sang giai đoạn InSeason
                    state.CurrentPhase = GamePhase.InSeason;

                    // Hệ thống sẽ tự động tạo giải đau và lịch thi đấu cho mùa giải mới
                    // ( Hiện tại Admin đang làm thủ công, chưa có tự động)

                    // Dựa vào lịch thi đấu, Bot sẽ tạo lineup cho các đội bóng do Bot quản lý ở vòng 1
                    await _matchService.PrepareRoundForAllTournamentAsync(state.CurrentSeason); //Lấy tẩt cả các vòng đấu trong mùa giải hiện tại

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
