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
        public string Position { get; set; } = string.Empty;
        public int Quality { get; set; }
        public int? ClubId { get; set; }
        public Club? Club { get; set; }
        public int ContractYears { get; set; }
        public string Status { get; set; } = "Trẻ"; // "Trẻ", "Trỗi dậy", "Đỉnh cao" , "Ổn định" , "Lão tướng", "Giải nghệ"
        public int AwardQBV { get; set; }
        public int AwardQBB { get; set; }
        public int AwardQBD { get; set; }
        public bool IsTransferListed { get; set; }


        // Navigation
        public ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
    }
}
