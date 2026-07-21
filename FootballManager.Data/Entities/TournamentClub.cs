namespace FootballManager.Data.Entities
{
    public class TournamentClub
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }
        public int ClubId { get; set; }
        public Club? Club { get; set; }

        // Standing fields
        public int Played { get; set; } = 0;
        public int Won { get; set; } = 0;
        public int Drawn { get; set; } = 0;
        public int Lost { get; set; } = 0;

        public int Points { get; set; }
        public int Rank { get; set; }        // Seed ban đầu (1 = mạnh nhất) dùng để ghép cặp knockout
        public int? Group { get; set; }       // Bảng đấu (vd 1/2) cho giải có vòng bảng như C1
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
    }
}