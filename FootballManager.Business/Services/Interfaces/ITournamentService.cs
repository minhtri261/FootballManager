using FootballManager.Business.DTOs;

namespace FootballManager.Business.Services.Interfaces
{
    public interface ITournamentService
    {
        Task<TournamentDetailDto?> GetTournamentDetailAsync(int tournamentId);
    }
}
