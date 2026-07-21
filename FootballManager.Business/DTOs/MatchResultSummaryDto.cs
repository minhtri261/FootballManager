using FootballManager.Data.Entities;

namespace FootballManager.Business.DTOs
{
    public class MatchResultSummaryDto
    {
        public int MatchId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int Week { get; set; }
        public int Round { get; set; }
        public bool IsHome { get; set; }
        public string? OpponentClubName { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public MatchResult Result { get; set; }
    }
}
