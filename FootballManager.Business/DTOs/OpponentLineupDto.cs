namespace FootballManager.Business.DTOs
{
    public class OpponentLineupDto
    {
        public int MatchId { get; set; }
        public int OpponentClubId { get; set; }
        public string OpponentClubName { get; set; } = string.Empty;
        public string Formation { get; set; } = string.Empty;
        public List<OpponentPlayerDto> Players { get; set; } = new();
    }

    public class OpponentPlayerDto
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Nation { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }
}
