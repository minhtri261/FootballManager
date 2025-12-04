namespace FootballManager.Data.Entities
{
    public class MatchGoal
    {
        public int Id { get; set; }

        public int MatchId { get; set; }
        public Match Match { get; set; }

        public int FootballerId { get; set; }
        public Footballer Footballer { get; set; }

        public int ClubId { get; set; }  // Để biết ai ghi cho đội nào
        public Club Club { get; set; }

        public bool IsOwnGoal { get; set; } = false; // Bàn phản lưới
    }
}
