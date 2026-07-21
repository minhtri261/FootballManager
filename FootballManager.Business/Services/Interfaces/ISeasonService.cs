using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface ISeasonService
    {
        // Khởi tạo 1 mùa giải: clone Tournament (nếu mùa > 1), gán CLB vào giải, sinh lịch League + Round 1 Cup/C1
        Task InitializeSeasonAsync(int seasonNumber);

        // Đảm bảo Match của round này đã tồn tại trước khi lấy ra mô phỏng (sinh động cho Cup/C1)
        Task EnsureRoundReadyAsync(TournamentType type, int round, int seasonNumber);

        // Thứ hạng cuối cùng của giải (ClubId theo rank 1..N) — League theo BXH, Cup/C1 theo kết quả bracket thực tế
        Task<List<int>> GetFinalRankingAsync(Tournament tournament, int seasonNumber);

        // Lưu SeasonSummary (vô địch/vua phá lưới/MVP) cho từng giải + trao QBV/QBB/QBĐ cho top 3 cầu thủ hay nhất mùa.
        // Trả về FootballerId đoạt QBV mùa này (null nếu không có).
        Task<int?> FinalizeSeasonAwardsAsync(int seasonNumber);

        // Cuối mùa: +1 Age, phân loại lại PlayerLifeCycle, tăng chỉ số theo TrainingQuality của CLB (Youth>Rising>Peak>Stable),
        // Veteran giảm chỉ số theo tuổi (giải nghệ nếu <=0), thưởng +1 Quality cho người vừa đoạt QBV mùa này.
        Task ApplyPlayerDevelopmentAsync(int? qbvWinnerFootballerId);

        // Cuối mùa: mỗi CLB được đôn 1 cầu thủ trẻ mới (16-18 tuổi), tên ghép ngẫu nhiên theo kho tên quốc gia,
        // Quality phân phối quanh Club.YouthTrainingQuality.
        Task PromoteYouthPlayersAsync(int seasonNumber);
    }
}
