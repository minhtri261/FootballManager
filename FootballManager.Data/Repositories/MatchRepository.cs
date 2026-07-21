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

        //Xem tất cả trận đấu thuộc giải đấu, sắp theo Round rồi Id (đúng thứ tự bracket)
        public async Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId)
        {
            return await _context.Matches
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.Round).ThenBy(m => m.Id)
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

        // Lấy tất cả trận đấu của 1 giải ở đúng round (dùng để check idempotency & lấy kết quả round trước).
        // Sắp theo Id (= đúng thứ tự sinh cặp theo seed ban đầu) để việc ghép cặp vòng sau luôn đúng nhánh bracket.
        public async Task<List<Match>> GetMatchesByTournamentAndRoundAsync(int tournamentId, int round)
        {
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId && m.Round == round)
                .OrderBy(m => m.Id)
                .ToListAsync();
        }

        // Trận chưa đá gần nhất (Week nhỏ nhất) của 1 CLB, dùng để hiển thị & nộp đội hình
        public async Task<Match?> GetNextMatchForClubAsync(int clubId)
        {
            return await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Include(m => m.MatchLineups)
                    .ThenInclude(ml => ml.Players)
                        .ThenInclude(p => p.Footballer)
                .Where(m => !m.IsPlayed && (m.HomeClubId == clubId || m.AwayClubId == clubId))
                .OrderBy(m => m.Week)
                .FirstOrDefaultAsync();
        }

        // Trận đã đá gần nhất (Week lớn nhất) của 1 CLB
        public async Task<Match?> GetLastPlayedMatchForClubAsync(int clubId)
        {
            return await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Include(m => m.Goals)
                .Where(m => m.IsPlayed && (m.HomeClubId == clubId || m.AwayClubId == clubId))
                .OrderByDescending(m => m.Week)
                .FirstOrDefaultAsync();
        }

        // Cầu thủ ghi nhiều bàn nhất (loại phản lưới nhà) trong 1 giải + mùa
        public async Task<int?> GetTopScorerAsync(int tournamentId, int seasonNumber)
        {
            return await _context.MatchGoals
                .Where(g => !g.IsOwnGoal && g.Match.TournamentId == tournamentId && g.Match.SeasonNumber == seasonNumber)
                .GroupBy(g => g.FootballerId)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefaultAsync();
        }

        // Cầu thủ nhiều lần MVP từng trận nhất trong 1 giải + mùa
        public async Task<int?> GetTournamentMvpAsync(int tournamentId, int seasonNumber)
        {
            return await _dbSet
                .Where(m => m.TournamentId == tournamentId && m.SeasonNumber == seasonNumber && m.MVPFootballerId != null)
                .GroupBy(m => m.MVPFootballerId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();
        }

        // FootballerId -> số lần MVP từng trận, tính trên toàn bộ mùa (mọi giải)
        public async Task<Dictionary<int, int>> GetMvpCountsBySeasonAsync(int seasonNumber)
        {
            return await _dbSet
                .Where(m => m.SeasonNumber == seasonNumber && m.MVPFootballerId != null)
                .GroupBy(m => m.MVPFootballerId!.Value)
                .Select(g => new { FootballerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FootballerId, x => x.Count);
        }

        public async Task SimulateMatchAsync(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.HomeClub)
                .Include(m => m.AwayClub)
                .Include(m => m.MatchLineups)
                    .ThenInclude(ml => ml.Players)
                        .ThenInclude(p => p.Footballer)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return;

            var rand = Random.Shared; // tránh tạo nhiều Random() liên tiếp trong vòng lặp mô phỏng nhiều trận, có thể trùng seed

            // 1. Tính sức mạnh tấn công / phòng thủ từ đội hình
            var (homeAtk, homeDef) = GetTeamStrengths(match.MatchLineups, match.HomeClubId);
            var (awayAtk, awayDef) = GetTeamStrengths(match.MatchLineups, match.AwayClubId ?? 0);

            // Lợi thế sân nhà +10% + phong độ CLB (mỗi điểm Form = 1% sức mạnh)
            homeAtk *= 1.1 * (1 + (match.HomeClub?.Form ?? 0) / 100.0);
            homeDef *= 1.05 * (1 + (match.HomeClub?.Form ?? 0) / 100.0);
            awayAtk *= 1 + (match.AwayClub?.Form ?? 0) / 100.0;
            awayDef *= 1 + (match.AwayClub?.Form ?? 0) / 100.0;

            // 2. Expected goals = 1.2 * (attack / opponent_defense), chuẩn hoá quanh 50
            double homeExpected = 1.2 * (homeAtk / awayDef);
            double awayExpected = 1.2 * (awayAtk / homeDef);

            match.HomeGoals = SimulateGoals(rand, homeExpected);
            match.AwayGoals = SimulateGoals(rand, awayExpected);
            match.IsPlayed = true;

            if (match.HomeGoals > match.AwayGoals) match.Result = MatchResult.HomeWin;
            else if (match.HomeGoals < match.AwayGoals) match.Result = MatchResult.AwayWin;
            else match.Result = MatchResult.Draw;

            // 2b. Cập nhật phong độ CLB: thắng +2, thua -2, hoà giữ nguyên (giới hạn [-20, 20])
            int homeFormDelta = match.Result == MatchResult.HomeWin ? 2 : match.Result == MatchResult.AwayWin ? -2 : 0;
            if (match.HomeClub != null)
                match.HomeClub.Form = Math.Clamp(match.HomeClub.Form + homeFormDelta, -20, 20);
            if (match.AwayClub != null)
                match.AwayClub.Form = Math.Clamp(match.AwayClub.Form - homeFormDelta, -20, 20);

            // 3. Cập nhật bảng xếp hạng
            await UpdateClubStandingAsync(match.TournamentId, match.HomeClubId, match.HomeGoals, match.AwayGoals);
            await UpdateClubStandingAsync(match.TournamentId, match.AwayClubId, match.AwayGoals, match.HomeGoals);

            // 4. Ghi nhận người ghi bàn (nếu có lineup) + xác định MVP trận đấu
            var homeLineup = match.MatchLineups.FirstOrDefault(l => l.ClubId == match.HomeClubId);
            var awayLineup = match.AwayClubId.HasValue ? match.MatchLineups.FirstOrDefault(l => l.ClubId == match.AwayClubId) : null;

            var goals = new List<MatchGoal>();
            for (int i = 0; i < match.HomeGoals; i++)
            {
                var scorer = PickScorer(homeLineup, rand);
                if (scorer != null)
                    goals.Add(new MatchGoal { MatchId = match.Id, FootballerId = scorer.Id, ClubId = match.HomeClubId });
            }
            for (int i = 0; i < match.AwayGoals; i++)
            {
                var scorer = PickScorer(awayLineup, rand);
                if (scorer != null && match.AwayClubId.HasValue)
                    goals.Add(new MatchGoal { MatchId = match.Id, FootballerId = scorer.Id, ClubId = match.AwayClubId.Value });
            }
            if (goals.Any())
                await _context.MatchGoals.AddRangeAsync(goals);

            match.MVPFootballerId = DetermineMatchMvp(match, goals, homeLineup, awayLineup);

            await _context.SaveChangesAsync();
        }

        // Trọng số ghi bàn theo nhóm vị trí (càng cao càng dễ được chọn ghi bàn)
        private static readonly Dictionary<PlayerPosition, double> GoalPositionWeight = new()
        {
            [PlayerPosition.ST] = 5.0,
            [PlayerPosition.AM] = 3.0,
            [PlayerPosition.LW] = 3.0,
            [PlayerPosition.RW] = 3.0,
            [PlayerPosition.CM] = 1.5,
            [PlayerPosition.DM] = 1.0,
            [PlayerPosition.CB] = 0.3,
            [PlayerPosition.LB] = 0.3,
            [PlayerPosition.RB] = 0.3,
            [PlayerPosition.GK] = 0.05,
        };

        // Chọn ngẫu nhiên có trọng số 1 cầu thủ ghi bàn từ đội hình ra sân (trọng số = vị trí * Quality)
        private static Footballer? PickScorer(MatchLineup? lineup, Random rand)
        {
            if (lineup == null) return null;
            var candidates = lineup.Players
                .Where(p => p.Footballer != null)
                .Select(p => (Player: p.Footballer!, Weight: GoalPositionWeight.GetValueOrDefault(p.Footballer!.Position, 0.5) * Math.Max(1, p.Footballer!.Quality)))
                .ToList();
            if (!candidates.Any()) return null;

            double total = candidates.Sum(c => c.Weight);
            double roll = rand.NextDouble() * total;
            double cumulative = 0;
            foreach (var c in candidates)
            {
                cumulative += c.Weight;
                if (roll <= cumulative) return c.Player;
            }
            return candidates[^1].Player;
        }

        // MVP = cầu thủ ghi nhiều bàn nhất trận (tie-break Quality); nếu không ai ghi bàn -> Quality cao nhất bên thắng (hoặc cả 2 bên nếu hoà)
        private static int? DetermineMatchMvp(Match match, List<MatchGoal> goals, MatchLineup? homeLineup, MatchLineup? awayLineup)
        {
            if (goals.Any())
            {
                var scorers = goals
                    .GroupBy(g => g.FootballerId)
                    .Select(g => new { FootballerId = g.Key, Count = g.Count() })
                    .ToList();

                var allPlayers = (homeLineup?.Players ?? Enumerable.Empty<MatchLineupPlayer>())
                    .Concat(awayLineup?.Players ?? Enumerable.Empty<MatchLineupPlayer>())
                    .Where(p => p.Footballer != null)
                    .ToDictionary(p => p.FootballerId, p => p.Footballer!.Quality);

                return scorers
                    .OrderByDescending(s => s.Count)
                    .ThenByDescending(s => allPlayers.GetValueOrDefault(s.FootballerId, 0))
                    .First().FootballerId;
            }

            IEnumerable<MatchLineupPlayer>? pool = match.Result switch
            {
                MatchResult.HomeWin => homeLineup?.Players,
                MatchResult.AwayWin => awayLineup?.Players,
                _ => (homeLineup?.Players ?? Enumerable.Empty<MatchLineupPlayer>()).Concat(awayLineup?.Players ?? Enumerable.Empty<MatchLineupPlayer>())
            };

            return pool?.Where(p => p.Footballer != null)
                .OrderByDescending(p => p.Footballer!.Quality)
                .FirstOrDefault()?.FootballerId;
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
