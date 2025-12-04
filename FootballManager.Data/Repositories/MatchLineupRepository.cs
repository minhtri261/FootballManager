using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Repositories
{
    public class MatchLineupRepository : BaseRepository<MatchLineup>, IMatchLineupRepository
    {
        public MatchLineupRepository(FootballContext context) : base(context) 
        {
        }

        public async Task<List<MatchLineupPlayer>> GetLineupPlayersAsync(int tournamentId, int matchId, int clubId)
        {
            return await _context.MatchLineupPlayers
                .Include(mlp => mlp.Footballer)
                .Include(mlp => mlp.MatchLineup)
                .Where(mlp =>
                    mlp.MatchLineup.Match.TournamentId == tournamentId &&
                    mlp.MatchLineup.MatchId == matchId &&
                    mlp.MatchLineup.ClubId == clubId)
                .ToListAsync();
        }

        public async Task<MatchLineup?> GetByMatchAndClubAsync(int matchId, int clubId)
        {
            return await _context.MatchLineups
                .Include(x => x.Club)
                .Include(x => x.Players)
                .ThenInclude(p => p.Footballer)
                .FirstOrDefaultAsync(x =>
                    x.MatchId == matchId &&
                    x.ClubId == clubId);
        }


    }
}
