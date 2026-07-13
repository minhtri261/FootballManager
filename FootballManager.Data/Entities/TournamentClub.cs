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
        public int Rank { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
    }
}