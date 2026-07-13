using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.DTOs
{
    public class RoundStateDto
    {
        public int Round { get; set; }
        public string State { get; set; } // WaitingForLineup | Finished
    }

}
