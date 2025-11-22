using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class Transfer
    {
        public int Id { get; set; }
        public int FootballerId { get; set; }
        public Footballer? Footballer { get; set; }
        public int? FromClubId { get; set; }
        public Club? FromClub { get; set; }
        public int ToClubId { get; set; }
        public Club? ToClub { get; set; }
        public decimal TransferFee { get; set; }
        public int ContractYears { get; set; }

        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected"
    }
}
