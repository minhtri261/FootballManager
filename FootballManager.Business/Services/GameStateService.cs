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

        public GameStateService(
            IGameStateRepository gameRepo,
            IMatchRepository matchRepo,
            ITournamentRepository tournamentRepo,
            IUnitOfWork unitOfWork,
            IClubRepository clubRepo,
            IBotLineupService botService)
        {
            _gameRepo = gameRepo;
            _matchRepo = matchRepo;
            _tournamentRepo = tournamentRepo;
            _unitOfWork = unitOfWork;
            _clubRepo = clubRepo;
            _botService = botService;
        }

        public async Task<GameStepResultDto> AdvanceNextWeekAsync()
        {
            var state = await _gameRepo.GetCurrentStateAsync();
            if (state == null) throw new Exception("GameState chưa được khởi tạo.");

            var template = await _gameRepo.GetTemplateByWeekAsync(state.CurrentWeek);
            if (template == null) throw new Exception("Không tìm thấy Schedule Template.");

            var result = new GameStepResultDto { FinishedWeek = state.CurrentWeek };

            if (state.CurrentWeek == 3)
            {
                await _botService.SetupBotLineupsForSeasonAsync(state.CurrentSeason);
            }

            // 1. Logic thi đấu
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
            }

            return result;
        }

        private async Task FinalizeSeasonAsync(GameState state)
        {
            // 1. Lấy tất cả các giải đấu của mùa giải hiện tại
            var tournaments = await _tournamentRepo.GetTournamentsBySeasonAsync(state.CurrentSeason);

            foreach (var tournament in tournaments)
            {
                // Lấy bảng xếp hạng cuối cùng của giải này
                var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id);

                // Parse RewardByRank từ JSON
                var rewards = new List<RankRewardDto>();
                if (!string.IsNullOrEmpty(tournament.RewardByRank))
                {
                    rewards = JsonSerializer.Deserialize<List<RankRewardDto>>(tournament.RewardByRank,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                // Trao thưởng cho từng CLB dựa trên vị trí (Rank)
                for (int i = 0; i < standings.Count; i++)
                {
                    var standing = standings[i];
                    var rank = i + 1; // Vị trí 1, 2, 3...

                    var reward = rewards.FirstOrDefault(r => r.Rank == rank);
                    if (reward != null && reward.Money > 0)
                    {
                        var club = await _clubRepo.GetByIdAsync(standing.ClubId);
                        if (club != null)
                        {
                            club.Money += reward.Money;
                            _clubRepo.Update(club);
                        }
                    }
                }
            }

            // 2. Tăng tuổi cầu thủ & Cập nhật trạng thái sự nghiệp
            // Để làm sau khi đã có dữ liệu về tuổi thọ sự nghiệp của cầu thủ

            // 3. Tiến sang mùa giải mới
            state.CurrentSeason++; 
            state.CurrentWeek = 1;
            state.CurrentPhase = GamePhase.PreSeason;
            _gameRepo.Update(state);

            // Lưu tất cả thay đổi trong một Transaction (UnitOfWork)
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<int> ProcessMatchesAsync(TournamentType type, int round)
        {
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
