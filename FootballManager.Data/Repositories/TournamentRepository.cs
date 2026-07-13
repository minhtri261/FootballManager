using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class TournamentRepository : BaseRepository<Tournament>, ITournamentRepository
    {
        public TournamentRepository(FootballContext context) : base(context) { }

        //Lấy BXH của giải đấu theo tournamentId và sắp xếp theo điểm, hiệu số bàn thắng bại, số bàn thắng
        public async Task<List<TournamentClub>> GetStandingsAsync(int tournamentId)
        {
            return await _context.TournamentClubs
                .Include(tc => tc.Club)
                .Where(tc => tc.TournamentId == tournamentId)
                .OrderByDescending(tc => tc.Points)
                .ThenByDescending(tc => (tc.GoalsFor - tc.GoalsAgainst))
                .ThenByDescending(tc => tc.GoalsFor)
                .ToListAsync();
        }

        //Lấy tất cả giải đấu theo seasonNumber
        public async Task<List<Tournament>> GetTournamentsBySeasonAsync(int seasonNumber)
        {
            return await _context.Tournaments
                .Where(t => t.SeasonNumber == seasonNumber)
                .ToListAsync();
        }
    }
}
