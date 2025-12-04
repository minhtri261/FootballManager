using FootballManager.Data.Entities;
namespace FootballManager.Business.DTOs
{
    public class MatchResultDto
    {
        public int MatchId { get; set; }
        public int HomeClubId { get; set; }
        public int AwayClubId { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public MatchResult Result { get; set; } // Enum: HomeWin / AwayWin / Draw
        public int? MVPFootballerId { get; set; } // Có thể null nếu chưa chọn
        public List<GoalDto>? Goals { get; set; } // Danh sách các bàn thắng
    }

    public class GoalDto
    {
        public int ClubId { get; set; }
        public int FootballerId { get; set; }
        public bool IsOwnGoal { get; set; }
    }
}
