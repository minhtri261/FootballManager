namespace FootballManagerMVC.Models
{
    public class RoundResultViewModel
    {
        public int TournamentId { get; set; }
        public List<MatchResultDto> Matches { get; set; }
        public List<StandingDto> Standings { get; set; }
    }
}
