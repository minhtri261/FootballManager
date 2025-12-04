using FootballManager.Data.Entities;

namespace FootballManager.Business.DTOs
{
    public class MatchSimulationDto
    {
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public int HomeClubId { get; set; }
        public int AwayClubId { get; set; }

        public MatchLineup? HomeLineup { get; set; }
        public MatchLineup? AwayLineup { get; set; }

        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
    }

}
