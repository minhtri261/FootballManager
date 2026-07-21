using FootballManager.Business.DTOs;
using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IMyClubService
    {
        Task<Club?> GetMyClubAsync();
        Task<IEnumerable<FootballerDto>> GetMyPlayersAsync();
        Task<NextMatchDto?> GetNextMatchAsync();
        Task<OpponentLineupDto?> GetNextOpponentLineupAsync();
        Task SubmitLineupAsync(SubmitLineupDto dto);
        Task<MatchResultSummaryDto?> GetLastMatchResultAsync();
    }
}
