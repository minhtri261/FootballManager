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
        public int YouthTrainingQuality { get; set; } = 1; // Chỉ số đào tạo trẻ, quyết định Quality cầu thủ trẻ đôn lên cuối mùa
        public bool IsBot { get; set; }
        public bool IsFinalized { get; set; }
        public int Form { get; set; } // Phong độ CLB, giới hạn [-20, 20], mặc định 0
        // Navigation
        public ICollection<Footballer> Footballers { get; set; } = new List<Footballer>();
        public ICollection<TournamentClub> TournamentClubs { get; set; } = new List<TournamentClub>();
        public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public ICollection<Match> AwayMatches { get; set; } = new List<Match>();

        public ICollection<MatchLineup> MatchLineups { get; set; } = new List<MatchLineup>();
    }
}
