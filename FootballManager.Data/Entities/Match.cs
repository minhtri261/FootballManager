using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }


        public int HomeClubId { get; set; }
        public Club? HomeClub { get; set; }


        public int? AwayClubId { get; set; }
        public Club? AwayClub { get; set; }


        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public MatchResult Result { get; set; }


        // Metadata cho lịch/đấu
        public int Round { get; set; }                // Vòng (1,2,...). Lượt về có thể là round + roundsCount
        public bool IsPlayed { get; set; } = false;  // Đã đá chưa
        public int? Leg { get; set; }                // 1=first leg,2=second leg
        public int? Group { get; set; }

        public int? MVPFootballerId { get; set; }
        public Footballer? MVPFootballer { get; set; }

        public ICollection<MatchLineup> MatchLineups { get; set; } = new List<MatchLineup>();

        public ICollection<MatchGoal> Goals { get; set; } = new List<MatchGoal>();
    }
    public enum MatchResult
    {
        HomeWin,
        AwayWin,
        Draw
    }
}
