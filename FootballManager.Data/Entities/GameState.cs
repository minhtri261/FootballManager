namespace FootballManager.Data.Entities
{
    public class GameState
    {
        public int Id { get; set; }
        public int CurrentSeason { get; set; }
        public GamePhase CurrentPhase { get; set; }
    }

    public enum GamePhase
    {
        PreSeason = 0,      // Giai đoạn 0 - Trước mùa giải
        TransferWindow = 1, // Giai đoạn 1 - Thị trường chuyển nhượng đầu mùa
        InSeason = 2,        // Giai đoạn 2 - Vào mùa giải
        SeasonSummary = 3    // Giai đoạn 3 - Tổng kết mùa giải
    }
}
