using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class Tournament
    {
        public int Id { get; set; }
        public int SeasonNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TeamsCount { get; set; }
        public TournamentType Type { get; set; }
        public int PlayersPerMatch { get; set; } = 7;


        // RewardByRank stored as JSON string (e.g. [{"rank":1,"money":2000},{"rank":2,"money":1500}])
        public string? RewardByRank { get; set; }


        // Navigation
        public ICollection<TournamentClub> TournamentClubs { get; set; } = new List<TournamentClub>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }

    public enum TournamentType
    {
        League,
        Cup,
        C1
    }
}
