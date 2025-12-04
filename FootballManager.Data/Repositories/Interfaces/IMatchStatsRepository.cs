using FootballManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface IMatchStatsRepository
    {
        Task<int> GetTotalGoalsAsync(int tournamentId, int clubId);
        Task<int> GetTotalConcededGoalsAsync(int tournamentId, int clubId);
        Task<int> GetCurrentRankAsync(int tournamentId, int clubId);
        Task<List<Footballer>> GetTopScorersAsync(int tournamentId, int top = 5);
        Task<int> GetGoalsRankAsync(int tournamentId, int clubId);         // 1 = most goals
        Task<int> GetConcededRankAsync(int tournamentId, int clubId);    // 1 = most conceded
    }

}
