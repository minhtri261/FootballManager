using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IMatchRepository : IGenericRepository<Match>
    {
        Task AddRangeAsync(IEnumerable<Match> matches);
        Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId);

        Task<List<Match>> GetUnplayedMatchesByTournamentAsync(int tournamentId);

        Task<List<Match>> GetLastFiveMatchesAsync(int tournamentId, int clubId);
        Task<List<Match>> GetLastRoundResultsAsync(int tournamentId);

        Task<List<Match>> GetLastRoundResultsForClubAsync(int tournamentId, int clubId);
    }
}
