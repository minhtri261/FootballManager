using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        public MatchRepository(FootballContext context) : base(context) { }
        public async Task AddRangeAsync(IEnumerable<Match> matches)
        {
            await _dbSet.AddRangeAsync(matches);
            await _context.SaveChangesAsync();
        }

        //Xem tất cả trận đấu thuộc giải đấu
        public async Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId)
        {
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId)
                .ToListAsync();
        }

        //Xem tất cả trận đấu thuộc giải đấu nhưng chưa đá
        public async Task<IEnumerable<Match>> GetPendingMatchesAsync(TournamentType type, int round)
        {
            return await _context.Matches
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Where(m => m.Tournament.Type == type
                       && m.Round == round
                       && !m.IsPlayed)
                .ToListAsync();
        }

        //Xem tất cả trận đấu của 1 CLB trong mùa này
        public async Task<List<Match>> GetMatchesByClubAndSeasonAsync(int clubId, int seasonNumber)
        {
            return await _dbSet
                .Where(m => m.SeasonNumber == seasonNumber &&
                            (m.HomeClubId == clubId || m.AwayClubId == clubId))
                .ToListAsync();
        }

        //Xem kết quả 1 đội cụ thể của Round vừa đá xong
        public async Task<List<Match>> GetLastRoundResultsForClubAsync(int tournamentId, int clubId)
        {
            // Bước 1: Tìm số Round lớn nhất mà CHÍNH CLB NÀY đã tham gia và đã đá xong
            int? lastRoundMyClub = await _dbSet
                .Where(m => m.TournamentId == tournamentId &&
                            m.IsPlayed &&
                            (m.HomeClubId == clubId || m.AwayClubId == clubId))
                .MaxAsync(m => (int?)m.Round);

            if (lastRoundMyClub == null)
                return new List<Match>();

            // Bước 2: Lấy thông tin trận đấu đó
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId &&
                            m.IsPlayed &&
                            m.Round == lastRoundMyClub && // Dùng đúng round của chính CLB
                            (m.HomeClubId == clubId || m.AwayClubId == clubId))
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Include(m => m.Goals) // Đừng quên include Goals để hiện tỉ số chi tiết
                .ToListAsync();
        }

        public async Task SimulateMatchAsync(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.MatchLineups)
                    .ThenInclude(ml => ml.Players)
                        .ThenInclude(p => p.Footballer)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return;

            var rand = new Random();

            // 1. Tính sức mạnh tấn công / phòng thủ từ đội hình
            var (homeAtk, homeDef) = GetTeamStrengths(match.MatchLineups, match.HomeClubId);
            var (awayAtk, awayDef) = GetTeamStrengths(match.MatchLineups, match.AwayClubId ?? 0);

            // Lợi thế sân nhà +10%
            homeAtk *= 1.1;
            homeDef *= 1.05;

            // 2. Expected goals = 1.2 * (attack / opponent_defense), chuẩn hoá quanh 50
            double homeExpected = 1.2 * (homeAtk / awayDef);
            double awayExpected = 1.2 * (awayAtk / homeDef);

            match.HomeGoals = SimulateGoals(rand, homeExpected);
            match.AwayGoals = SimulateGoals(rand, awayExpected);
            match.IsPlayed = true;

            if (match.HomeGoals > match.AwayGoals) match.Result = MatchResult.HomeWin;
            else if (match.HomeGoals < match.AwayGoals) match.Result = MatchResult.AwayWin;
            else match.Result = MatchResult.Draw;

            // 3. Cập nhật bảng xếp hạng
            await UpdateClubStandingAsync(match.TournamentId, match.HomeClubId, match.HomeGoals, match.AwayGoals);
            await UpdateClubStandingAsync(match.TournamentId, match.AwayClubId, match.AwayGoals, match.HomeGoals);

            await _context.SaveChangesAsync();
        }

        // Tính (attackStrength, defenseStrength) từ đội hình ra sân
        private (double attack, double defense) GetTeamStrengths(ICollection<MatchLineup> lineups, int clubId)
        {
            var lineup = lineups.FirstOrDefault(l => l.ClubId == clubId);
            if (lineup == null || !lineup.Players.Any())
                return (50.0, 50.0);

            var players = lineup.Players.Where(p => p.Footballer != null).ToList();

            var attackers = players.Where(p => p.Footballer!.Position is
                PlayerPosition.ST or PlayerPosition.AM or PlayerPosition.LW or PlayerPosition.RW).ToList();

            var midfielders = players.Where(p => p.Footballer!.Position is
                PlayerPosition.CM or PlayerPosition.DM).ToList();

            var defenders = players.Where(p => p.Footballer!.Position is
                PlayerPosition.GK or PlayerPosition.CB or PlayerPosition.LB or PlayerPosition.RB).ToList();

            double atkBase = attackers.Any() ? attackers.Average(p => p.Footballer!.Quality) : 50.0;
            double defBase = defenders.Any() ? defenders.Average(p => p.Footballer!.Quality) : 50.0;

            // Tiền vệ đóng góp 30% vào cả tấn công lẫn phòng thủ
            if (midfielders.Any())
            {
                double midAvg = midfielders.Average(p => p.Footballer!.Quality);
                atkBase = atkBase * 0.7 + midAvg * 0.3;
                defBase = defBase * 0.7 + midAvg * 0.3;
            }

            return (atkBase, defBase);
        }

        // Xấp xỉ phân phối Poisson để tạo số bàn thắng ngẫu nhiên
        private static int SimulateGoals(Random rand, double expected)
        {
            double L = Math.Exp(-expected);
            int k = 0;
            double p = 1.0;
            do
            {
                k++;
                p *= rand.NextDouble();
            } while (p > L);
            return Math.Min(k - 1, 8);
        }

        private async Task UpdateClubStandingAsync(int tournamentId, int? clubId, int goalsFor, int goalsAgainst)
        {
            var standing = await _context.TournamentClubs
                .FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.ClubId == clubId);

            if (standing == null) return;

            standing.Played += 1;
            standing.GoalsFor += goalsFor;
            standing.GoalsAgainst += goalsAgainst;

            if (goalsFor > goalsAgainst)
            {
                standing.Won += 1;
                standing.Points += 3;
            }
            else if (goalsFor == goalsAgainst)
            {
                standing.Drawn += 1;
                standing.Points += 1;
            }
            else
            {
                standing.Lost += 1;
            }
        }
    }
}
