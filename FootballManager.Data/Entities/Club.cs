using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class Club
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nation { get; set; } = string.Empty;
        public decimal Money { get; set; }
        public int LeagueCups { get; set; }
        public int ChampionsCups { get; set; }
        public int NationalCups { get; set; }
        public int TrainingQuality { get; set; }
        public bool IsBot { get; set; }
        public bool IsFinalized { get; set; } = false;
        // Navigation
        public ICollection<Footballer> Footballers { get; set; } = new List<Footballer>();
        public ICollection<TournamentClub> TournamentClubs { get; set; } = new List<TournamentClub>();
        public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    }
}
