namespace FootballManager.Business.DTOs
{
    public class TournamentDTO
    {
        public int Id { get; set; }
        public int SeasonNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TeamsCount { get; set; }
        public string Type { get; set; } = string.Empty;
        public int PlayersPerMatch { get; set; }
        public List<RewardRankDTO>? RewardByRank { get; set; }
    }

    public class RewardRankDTO
    {
        public int Rank { get; set; }
        public int Money { get; set; }
    }

    public class TournamentClubDTO
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int Rank { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
    }

    public class TournamentWithClubsDTO : TournamentDTO
    {
        public List<TournamentClubDTO> Clubs { get; set; } = new();
    }
}
