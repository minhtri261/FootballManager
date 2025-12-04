using FootballManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IMatchLineupRepository : IGenericRepository<MatchLineup>
    {
        Task<List<MatchLineupPlayer>> GetLineupPlayersAsync(int tournamentId, int matchId, int opponentClubId);

        Task<MatchLineup?> GetByMatchAndClubAsync(int matchId, int clubId);

    }
}
