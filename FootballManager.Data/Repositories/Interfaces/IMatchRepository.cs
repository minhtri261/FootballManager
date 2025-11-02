using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IMatchRepository : IGenericRepository<Match>
    {
        Task AddRangeAsync(IEnumerable<Match> matches);
        Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId);
    }
}
