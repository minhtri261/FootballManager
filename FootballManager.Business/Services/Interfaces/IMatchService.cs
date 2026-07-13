using FootballManager.Business.DTOs;
using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IMatchService : IBaseService<Match>
    {
        Task<List<int>> PrepareRoundForAllTournamentAsync(int seasonNumber);

        Task SaveMatchLineupAsync(SubmitLineupDto dto);

        Task<OpponentLineupDto> GetOpponentLineupAsync(int matchId);

        Task ApplySimulationResultAsync(MatchSimulationDto dto);

        Task<List<MatchDto>> GetCurrentRoundMatchesAsync(int tournamentId);

        Task<List<MatchResultDto>> GetLastRoundResultsAsync(int tournamentId);
        Task<MyMatchResultDto> MyClubResultLastRound(int tournamentId);

        Task<RoundStateDto> GetCurrentRoundStateAsync(int tournamentId);
    }
}
