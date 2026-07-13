namespace FootballManagerMVC.Models
{
    public class DashboardViewModel
    {
        public GameStateDashVm GameState { get; set; } = new();
        public ClubDashVm Club { get; set; } = new();
        public List<PlayerDashVm> Players { get; set; } = new();
        public List<TournamentDashVm> Tournaments { get; set; } = new();
        public MatchDashVm? NextMatch { get; set; }
        public List<MatchDashVm> FinishedMatches { get; set; } = new();
    }

    public class GameStateDashVm
    {
        public int CurrentSeason { get; set; }
        public int CurrentWeek { get; set; }
        public int CurrentPhase { get; set; }
        public string PhaseName { get; set; } = "";
    }

    public class ClubDashVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Nation { get; set; } = "";
        public decimal Money { get; set; }
        public int LeagueCups { get; set; }
        public int ChampionsCups { get; set; }
        public int NationalCups { get; set; }
        public int TrainingQuality { get; set; }
    }

    public class PlayerDashVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Nation { get; set; } = "";
        public string Position { get; set; } = "";
        public int Quality { get; set; }
        public int Potential { get; set; }
        public int ContractYears { get; set; }
        public string Status { get; set; } = "";
        public bool IsTransferListed { get; set; }
    }

    public class TournamentDashVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public List<StandingDashVm> Standings { get; set; } = new();
    }

    public class StandingDashVm
    {
        public int Rank { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = "";
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public bool IsMyClub { get; set; }
    }

    public class MatchDashVm
    {
        public int MatchId { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = "";
        public int Round { get; set; }
        public int HomeClubId { get; set; }
        public string HomeClubName { get; set; } = "";
        public int AwayClubId { get; set; }
        public string AwayClubName { get; set; } = "";
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public string? Result { get; set; }
        public bool IsHome { get; set; }
        public bool IsPlayed { get; set; }
    }
}
