using System.Text.Json;
using System.Text.Json.Serialization;

namespace FootballManagerMVC.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SeasonNumber { get; set; }
        public int TeamsCount { get; set; }
        public string Type { get; set; } = string.Empty;
        public int PlayersPerMatch { get; set; }
        public List<RewardRank>? RewardByRank { get; set; }
        public List<TournamentClub>? Clubs { get; set; }

        public List<Match>? Matches { get; set; }
    }

    public class RewardRank
    {
        [JsonPropertyName("rank")]
        public int Rank { get; set; }
        [JsonPropertyName("money")]
        public int Money { get; set; }
    }

    public class TournamentClub
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int Rank { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
    }
}
