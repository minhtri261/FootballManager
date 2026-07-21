namespace FootballManager.Business.DTOs
{
    public class NextMatchDto
    {
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int Week { get; set; }
        public int Round { get; set; }
        public bool IsHome { get; set; }
        public int? OpponentClubId { get; set; }
        public string? OpponentClubName { get; set; }
        public bool HasSubmittedLineup { get; set; }
        public int PlayersPerMatch { get; set; }
    }
}
