namespace FootballManager.Business.DTOs
{
    public class SubmitLineupDto
    {
        public int MatchId { get; set; }
        public string Formation { get; set; } = string.Empty;
        public List<int> PlayerIds { get; set; } = new();
    }
}
