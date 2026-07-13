using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class Footballer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Nation { get; set; } = string.Empty;
        public PlayerPosition Position { get; set; }
        public int Quality { get; set; }
        public int Potential { get; set; }
        public int? ClubId { get; set; }
        public Club? Club { get; set; }
        public int ContractYears { get; set; }
        public PlayerLifeCycle Status { get; set; }
        public int AwardQBV { get; set; }
        public int AwardQBB { get; set; }
        public int AwardQBD { get; set; }
        public bool IsTransferListed { get; set; }


        // Navigation
        public ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
    }

    public enum PlayerLifeCycle { Youth, Rising, Peak, Stable, Veteran, Retired }

    public enum PlayerPosition
    {
        GK, // Goalkeeper
        CB, // Center Back
        LB, // Left Back
        RB, // Right Back
        DM, // Defensive Midfielder
        CM, // Central Midfielder
        AM, // Attacking Midfielder
        LW, // Left Winger
        RW, // Right Winger
        ST  // Striker
    }
}
