namespace FootballManagerMVC.Models
{
    public class MyMatchViewModel
    {
        public Match Match { get; set; }
        public OpponentLineupDto OpponentLineup { get; set; }
        public List<Footballer> MyPlayers { get; set; }
    }

}
