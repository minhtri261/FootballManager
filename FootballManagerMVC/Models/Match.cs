namespace FootballManagerMVC.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }


        public int HomeClubId { get; set; }
        public Club? HomeClub { get; set; }


        public int AwayClubId { get; set; }
        public Club? AwayClub { get; set; }


        public int? HomeGoals { get; set; }
        public int? AwayGoals { get; set; }
        public MatchResult Result { get; set; }
        public int? MVPFootballerId { get; set; }
        public Footballer? MVPFootballer { get; set; }
    }
    public enum MatchResult
    {
        HomeWin,
        AwayWin,
        Draw
    }
}
