using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IMatchRepository : IBaseRepository<Match>
    {
        Task AddRangeAsync(IEnumerable<Match> matches);
        Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId);
        Task<List<Match>> GetMatchesByClubAndSeasonAsync(int clubId, int seasonNumber);
        Task<IEnumerable<Match>> GetPendingMatchesAsync(TournamentType type, int round);
        Task<List<Match>> GetLastRoundResultsForClubAsync(int tournamentId, int clubId);
        Task SimulateMatchAsync(int matchId);
        Task<List<Match>> GetMatchesByTournamentAndRoundAsync(int tournamentId, int round);
        Task<Match?> GetNextMatchForClubAsync(int clubId);
        Task<Match?> GetLastPlayedMatchForClubAsync(int clubId);

        // Cầu thủ ghi nhiều bàn nhất (loại phản lưới nhà) trong 1 giải + mùa
        Task<int?> GetTopScorerAsync(int tournamentId, int seasonNumber);

        // Cầu thủ nhiều lần MVP từng trận nhất trong 1 giải + mùa
        Task<int?> GetTournamentMvpAsync(int tournamentId, int seasonNumber);

        // FootballerId -> số lần MVP từng trận, tính trên toàn bộ mùa (mọi giải)
        Task<Dictionary<int, int>> GetMvpCountsBySeasonAsync(int seasonNumber);

    }
}
