namespace FootballManagerMVC.Models
{
    public class SubmitLineupDto
    {
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string Formation { get; set; } = string.Empty;
        public List<int> PlayerIds { get; set; } = new();
    }
}
