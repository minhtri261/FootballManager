using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FootballManager.Business.Services
{
    public class GameStateService : IGameStateService
    {
        private readonly IGameStateRepository _gameRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMatchRepository _matchRepo;
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IBotLineupService _botService;
        private readonly ISeasonService _seasonService;
        private readonly ITransferService _transferService;

        public GameStateService(
            IGameStateRepository gameRepo,
            IMatchRepository matchRepo,
            ITournamentRepository tournamentRepo,
            IUnitOfWork unitOfWork,
            IClubRepository clubRepo,
            IBotLineupService botService,
            ISeasonService seasonService,
            ITransferService transferService)
        {
            _gameRepo = gameRepo;
            _matchRepo = matchRepo;
            _tournamentRepo = tournamentRepo;
            _unitOfWork = unitOfWork;
            _clubRepo = clubRepo;
            _botService = botService;
            _seasonService = seasonService;
            _transferService = transferService;
        }

        public async Task<GameStepResultDto> AdvanceNextWeekAsync()
        {
            var state = await _gameRepo.GetCurrentStateAsync();
            if (state == null) throw new Exception("GameState chưa được khởi tạo.");

            // Tuần 1 = tuần đầu mùa (chuyển nhượng) -> khởi tạo giải đấu/lịch thi đấu cho mùa này,
            // đồng thời Bot niêm yết cầu thủ dư thừa + gửi đề nghị mua. Người chơi có nguyên tuần 2 để tự
            // xem thị trường/gửi đề nghị của mình trước khi mọi giao dịch được chốt lúc rời giai đoạn chuyển nhượng.
            if (state.CurrentWeek == 1)
            {
                await _seasonService.InitializeSeasonAsync(state.CurrentSeason);
                await _transferService.BotListSurplusPlayersAsync();
                await _transferService.BotDecideWhoToBuyOrRenewAsync();
            }

            var template = await _gameRepo.GetTemplateByWeekAsync(state.CurrentWeek);
            if (template == null) throw new Exception("Không tìm thấy Schedule Template.");

            var result = new GameStepResultDto { FinishedWeek = state.CurrentWeek };

            // 1. Logic thi đấu — các trận của tuần này đã được sinh sẵn từ lượt gọi next-week TRƯỚC đó
            // (xem bước "look-ahead" bên dưới), nên người chơi luôn có ít nhất 1 lượt để xem & nộp đội hình
            // trước khi trận thực sự được mô phỏng ở đây.
            if (template.TournamentType.HasValue)
            {
                result.PlayedMatchesCount = await ProcessMatchesAsync(template.TournamentType.Value, template.Round);
                result.Message = $"Đã thi đấu xong Vòng {template.Round} giải {template.TournamentType}.";
            }

            // 2. Kiểm tra nếu tuần này được đánh dấu là kết thúc mùa (IsSeasonEnd)
            // Hoặc là tuần cuối cùng trong DB (Week 14 theo dữ liệu mẫu của bạn)
            if (template.IsSeasonEnd || template.Description.Contains("Tổng kết"))
            {
                await FinalizeSeasonAsync(state);
                result.Message = "Mùa giải đã kết thúc. Đã tổng kết tất cả các giải đấu và chuyển sang mùa mới.";
            }
            else
            {
                state.CurrentWeek++;
                var nextTemplate = await _gameRepo.GetTemplateByWeekAsync(state.CurrentWeek);
                state.CurrentPhase = DeterminePhase(nextTemplate);

                _gameRepo.Update(state);
                await _unitOfWork.SaveChangesAsync();

                // Look-ahead: sinh sẵn lịch (nếu cần, vd knockout vòng sau) cho TUẦN SẮP TỚI ngay bây giờ,
                // thay vì đợi tới lần next-week kế mới sinh rồi mô phỏng luôn trong cùng 1 lượt gọi.
                // Nhờ vậy người chơi luôn có thời gian giữa 2 lần gọi next-week để xem đối thủ & nộp đội hình.
                if (nextTemplate?.TournamentType != null)
                {
                    await _seasonService.EnsureRoundReadyAsync(nextTemplate.TournamentType.Value, nextTemplate.Round, state.CurrentSeason);
                }

                // Rời khỏi giai đoạn chuyển nhượng (tuần này còn mở, tuần sau đã đóng) -> chốt toàn bộ đề nghị
                // đang Pending (của cả Bot lẫn người chơi).
                if (template.IsTransferOpen && !(nextTemplate?.IsTransferOpen ?? false))
                {
                    await _transferService.BotDecideTransfersAsync();
                }
            }

            // BOT tự nộp đội hình cho mọi trận đang tồn tại nhưng chưa có lineup (kể cả trận vừa sinh ở bước look-ahead).
            // Idempotent — trận đã có lineup thì bỏ qua, nên gọi mỗi lần next-week không tốn kém/không đổi kết quả.
            await _botService.SetupBotLineupsForSeasonAsync(state.CurrentSeason);

            return result;
        }

        private async Task FinalizeSeasonAsync(GameState state)
        {
            // 1. Lấy tất cả các giải đấu của mùa giải hiện tại
            var tournaments = await _tournamentRepo.GetTournamentsBySeasonAsync(state.CurrentSeason);

            foreach (var tournament in tournaments)
            {
                // Thứ hạng cuối cùng của giải: League theo BXH, Cup/C1 theo kết quả bracket thực tế (ai thắng Chung kết)
                var ranking = await _seasonService.GetFinalRankingAsync(tournament, state.CurrentSeason);

                // Parse RewardByRank từ JSON
                var rewards = new List<RankRewardDto>();
                if (!string.IsNullOrEmpty(tournament.RewardByRank))
                {
                    rewards = JsonSerializer.Deserialize<List<RankRewardDto>>(tournament.RewardByRank,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                // Trao thưởng cho từng CLB dựa trên vị trí (Rank)
                for (int i = 0; i < ranking.Count; i++)
                {
                    var rank = i + 1; // Vị trí 1, 2, 3...

                    var reward = rewards.FirstOrDefault(r => r.Rank == rank);
                    if (reward != null && reward.Money > 0)
                    {
                        var club = await _clubRepo.GetByIdAsync(ranking[i]);
                        if (club != null)
                        {
                            club.Money += reward.Money;
                            _clubRepo.Update(club);
                        }
                    }
                }
            }

            // 2. Lưu SeasonSummary (vô địch/vua phá lưới/MVP từng giải) + trao QBV/QBB/QBĐ cho top 3 cầu thủ hay nhất mùa
            var qbvWinnerId = await _seasonService.FinalizeSeasonAwardsAsync(state.CurrentSeason);

            // 3. Tăng tuổi cầu thủ, cập nhật PlayerLifeCycle, tăng/giảm chỉ số, giải nghệ, thưởng QBV
            await _seasonService.ApplyPlayerDevelopmentAsync(qbvWinnerId);

            // 4. Đôn cầu thủ trẻ mới cho mỗi CLB
            await _seasonService.PromoteYouthPlayersAsync(state.CurrentSeason);

            // 5. Tiến sang mùa giải mới
            state.CurrentSeason++; 
            state.CurrentWeek = 1;
            state.CurrentPhase = GamePhase.PreSeason;
            _gameRepo.Update(state);

            // Lưu tất cả thay đổi trong một Transaction (UnitOfWork)
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<int> ProcessMatchesAsync(TournamentType type, int round)
        {
            // Lịch của round này đã được sinh sẵn (InitializeSeasonAsync ở tuần 1, hoặc bước look-ahead của
            // lần next-week trước) và BOT cũng đã có lineup — ở đây chỉ cần lấy trận chưa đấu & mô phỏng.
            var matches = await _matchRepo.GetPendingMatchesAsync(type, round);
            foreach (var m in matches)
            {
                await _matchRepo.SimulateMatchAsync(m.Id);
            }
            return matches.Count();
        }

        private GamePhase DeterminePhase(ScheduleTemplate? temp)
        {
            if (temp == null) return GamePhase.PreSeason;
            if (temp.IsSeasonEnd) return GamePhase.SeasonSummary;
            if (temp.IsTransferOpen) return GamePhase.TransferWindow;
            if (temp.TournamentType != null) return GamePhase.InSeason;
            return GamePhase.PreSeason;
        }
    }
    public class RankRewardDto
    {
        public int Rank { get; set; }
        public decimal Money { get; set; }
    }
}
