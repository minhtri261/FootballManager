namespace FootballManager.Data.Entities
{
    public class MatchLineup
    {
        public int Id { get; set; }

        public int MatchId { get; set; }
        public Match Match { get; set; }

        public int ClubId { get; set; }
        public Club Club { get; set; }

        // Ví dụ "3-2-1" / "2-3-1" / "2-2-2"
        public string Formation { get; set; } = string.Empty;

        // Navigation
        public ICollection<MatchLineupPlayer> Players { get; set; } = new List<MatchLineupPlayer>();
    }
}

