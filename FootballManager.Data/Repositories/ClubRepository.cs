using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace FootballManager.Data.Repositories
{
    public class ClubRepository : BaseRepository<Club>, IClubRepository
    {
        public ClubRepository(FootballContext context) : base(context) { }

        public async Task<Club?> GetWithPlayersAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Footballers)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        //Lấy danh sách cầu thủ theo ClubId
        public async Task<IEnumerable<Footballer>> GetPlayersByClubIdAsync(int clubId)
        {
            return await _context.Footballers
                .Where(p => p.ClubId == clubId)
                .ToListAsync();
        }

        //Lấy chi tiết cầu thủ theo Id
        public async Task<Footballer?> GetPlayerByIdAsync(int id)
        {
            return await _context.Footballers
                .Include(p => p.Club)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        //Đếm số cầu thủ trong một câu lạc bộ
        public async Task<int> CountByClubAsync(int clubId)
        {
            return await _context.Footballers.CountAsync(f => f.ClubId == clubId);
        }

        //Thay đổi Chốt đội hình của câu lạc bộ
        public async Task SetClubFinalizedAsync(int clubId, bool isFinalized)
        {
            var club = await _dbSet.FindAsync(clubId);
            if (club != null)
            {
                club.IsFinalized = isFinalized;
                await _context.SaveChangesAsync();

                if (isFinalized)
                {
                    // Reject tất cả transfer pending liên quan (From hoặc To)
                    var related = await _context.Transfers
                        .Where(t => t.Status == "Pending" && (t.FromClubId == clubId || t.ToClubId == clubId))
                        .ToListAsync();

                    if (related.Any())
                    {
                        foreach (var t in related)
                            t.Status = "Rejected";

                        _context.Transfers.UpdateRange(related);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        //Đếm số cầu thủ theo vị trí trong một câu lạc bộ
        public async Task<int> CountByPositionAsync(int clubId, string position)
        {
            return await _context.Footballers
                .Where(f => f.ClubId == clubId && f.Position == position)
                .CountAsync();
        }

    }

}
