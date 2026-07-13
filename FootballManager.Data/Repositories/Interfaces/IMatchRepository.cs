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

    }
}
