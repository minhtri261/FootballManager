using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface ITournamentRepository : IGenericRepository<Tournament>
    {
        Task<Tournament?> GetWithClubsAsync(int id);
        Task<List<Tournament>> GetBySeasonNumberAsync(int seasonNumber);

        Task<TournamentClub?> GetTournamentClubAsync(int tournamentId, int clubId);
    }
}
