using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface ITournamentRepository : IBaseRepository<Tournament>
    {
        // Lấy BXH của giải đấu theo tournamentId
        Task<List<TournamentClub>> GetStandingsAsync(int tournamentId);

        // Lấy danh sách các giải đấu theo seasonNumber
        Task<List<Tournament>> GetTournamentsBySeasonAsync(int seasonNumber);
     }
}
