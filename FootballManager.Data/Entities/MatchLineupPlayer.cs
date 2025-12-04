namespace FootballManager.Data.Entities
{
    public class MatchLineupPlayer
    {
        public int Id { get; set; }

        public int MatchLineupId { get; set; }
        public MatchLineup MatchLineup { get; set; }

        public int FootballerId { get; set; }
        public Footballer Footballer { get; set; }

    }
}
