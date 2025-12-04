using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IBotLineupService : IBaseService<MatchLineup>
    {
        Task<string> CreateBotLineupAsync(
            int tournamentId,
            int botClubId,
            int opponentClubId,
            int matchId
        );
    }

}
