using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class SeasonSummary
    {
        public int Id { get; set; }
        public int SeasonNumber { get; set; }
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }
        public int? ChampionClubId { get; set; }
        public Club? ChampionClub { get; set; }
        public int? TopScorerId { get; set; }
        public Footballer? TopScorer { get; set; }
        public int? MVPFootballerId { get; set; }
        public Footballer? MVPFootballer { get; set; }
    }
}
