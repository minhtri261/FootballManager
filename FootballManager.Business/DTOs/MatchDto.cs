using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.DTOs
{
    public class MatchDto
    {
        public int MatchId { get; set; }
        public int HomeClubId { get; set; }
        public int AwayClubId { get; set; }
        public bool IsPlayed { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
    }

    public class NextTournamentDto
    {
        public int TournamentId { get; set; }
        public int Round { get; set; }
    }


}
