using FootballManager.Data;
using FootballManager.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace FootballManager.Business.Services
{
    public class TournamentClubService
    {
        private readonly FootballContext _context;

        public TournamentClubService(FootballContext context)
        {
            _context = context;
        }

        // Admin thêm CLB vào giải đấu
        public async Task AddClubToTournamentAsync(int tournamentId, int clubId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentClubs)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                throw new Exception("Giải đấu không tồn tại.");

            var club = await _context.Clubs.FindAsync(clubId);
            if (club == null)
                throw new Exception("Câu lạc bộ không tồn tại.");

            bool exists = tournament.TournamentClubs.Any(tc => tc.ClubId == clubId);
            if (exists)
                throw new Exception("CLB này đã có trong giải đấu rồi.");

            // Thêm mới
            var tournamentClub = new TournamentClub
            {
                TournamentId = tournamentId,
                ClubId = clubId,
                Played = 0,
                Won = 0,
                Drawn = 0,
                Lost = 0,
                Points = 0,
                Rank = 0,
                GoalsFor = 0,
                GoalsAgainst = 0
            };

            _context.TournamentClubs.Add(tournamentClub);
            await _context.SaveChangesAsync();
        }

        //Hiển thị danh sách CLB của giải đấu
        public async Task<List<object>> GetClubsByTournamentAsync(int tournamentId)
        {
            return await _context.TournamentClubs
                .Include(tc => tc.Club)
                .Where(tc => tc.TournamentId == tournamentId)
                .Select(tc => new
                {
                    tc.ClubId,
                    tc.Club!.Name,
                    tc.Won,
                    tc.Drawn,
                    tc.Lost,
                    tc.Points,
                    tc.GoalsFor,
                    tc.GoalsAgainst,
                    tc.Rank
                })
                .ToListAsync<object>();
        }

    }
}
    
