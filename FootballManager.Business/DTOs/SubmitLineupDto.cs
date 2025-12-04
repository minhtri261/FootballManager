using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.DTOs
{
    public class SubmitLineupDto
    {
        public int MatchId { get; set; }

        public int TournamentId { get; set; }

        public string Formation { get; set; } = string.Empty; // "3-2-1"

        public List<int> PlayerIds { get; set; } = new(); 
    }

}
