using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class BotService : IBotLineupService
    {
        private readonly IClubRepository _clubRepo;
        private readonly IMatchRepository _matchRepo;
        private readonly IUnitOfWork _unitOfWork;

        public BotService(IClubRepository clubRepo, IMatchRepository matchRepo, IUnitOfWork unitOfWork)
        {
            _clubRepo = clubRepo;
            _matchRepo = matchRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task SetupBotLineupsForSeasonAsync(int seasonNumber)
        {
            // 1. Lấy tất cả Club là Bot
            var botClubs = await _clubRepo.GetBotClubsWithPlayersAsync();

            foreach (var club in botClubs)
            {
                // 2. Lấy tất cả trận đấu của Club này trong mùa giải
                var matches = await _matchRepo.GetMatchesByClubAndSeasonAsync(club.Id, seasonNumber);

                // 3. Mỗi Bot có một sơ đồ ưa thích ngẫu nhiên hoặc cố định
                string[] formations = { "3-2-1", "2-3-1", "2-2-2" };
                string favoredFormation = formations[club.Id % formations.Length];

                // 4. Lấy danh sách 7 cầu thủ tốt nhất (Top Quality)
                // Giả sử Footballer có thuộc tính Quality (chỉ số chung)
                var bestSeven = SelectBestLineup(club.Footballers.ToList(), favoredFormation);

                foreach (var match in matches)
                {
                    // Kiểm tra xem đã có Lineup cho trận này chưa để tránh trùng lặp
                    if (match.MatchLineups.Any(l => l.ClubId == club.Id)) continue;

                    var lineup = new MatchLineup
                    {
                        MatchId = match.Id,
                        ClubId = club.Id,
                        Formation = favoredFormation,
                        Players = bestSeven.Select(p => new MatchLineupPlayer
                        {
                            FootballerId = p.Id
                        }).ToList()
                    };

                    match.MatchLineups.Add(lineup);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public static class FormationHelper
        {
            // Gom nhóm các vị trí chi tiết vào nhóm chính
            public static readonly Dictionary<string, PlayerPosition[]> PositionGroups = new()
            {
                { "GK", new[] { PlayerPosition.GK } },
                { "DF", new[] { PlayerPosition.CB, PlayerPosition.LB, PlayerPosition.RB } },
                { "MF", new[] { PlayerPosition.DM, PlayerPosition.CM, PlayerPosition.AM, PlayerPosition.LW, PlayerPosition.RW } },
                { "ST", new[] { PlayerPosition.ST } }
            };

            // Định nghĩa số lượng từng nhóm cho các sơ đồ sân 7 phổ biến
            // Format: GK-DF-MF-ST
            public static Dictionary<string, int[]> GetFormationSchema(string formation) => formation switch
            {
                "3-2-1" => new Dictionary<string, int[]> { { "GK", new[] { 1 } }, { "DF", new[] { 3 } }, { "MF", new[] { 2 } }, { "ST", new[] { 1 } } },
                "2-3-1" => new Dictionary<string, int[]> { { "GK", new[] { 1 } }, { "DF", new[] { 2 } }, { "MF", new[] { 3 } }, { "ST", new[] { 1 } } },
                "2-2-2" => new Dictionary<string, int[]> { { "GK", new[] { 1 } }, { "DF", new[] { 2 } }, { "MF", new[] { 2 } }, { "ST", new[] { 2 } } },
                _ => new Dictionary<string, int[]> { { "GK", new[] { 1 } }, { "DF", new[] { 3 } }, { "MF", new[] { 2 } }, { "ST", new[] { 1 } } } // Default 3-2-1
            };
        }

        private List<Footballer> SelectBestLineup(List<Footballer> allPlayers, string formation)
        {
            var selectedIds = new HashSet<int>();
            var finalLineup = new List<Footballer>();
            var schema = FormationHelper.GetFormationSchema(formation);

            // 1. Duyệt qua từng nhóm (GK, DF, MF, ST) theo sơ đồ
            foreach (var slot in schema)
            {
                string groupName = slot.Key;
                int requiredCount = slot.Value[0];
                var allowedPositions = FormationHelper.PositionGroups[groupName];

                // Lấy những cầu thủ thuộc nhóm vị trí này chưa được chọn, sắp xếp theo Quality
                var bestInPosition = allPlayers
                    .Where(p => allowedPositions.Contains(p.Position) && !selectedIds.Contains(p.Id))
                    .OrderByDescending(p => p.Quality)
                    .ThenBy(p => p.Id) // tie-break ổn định để đội hình BOT không đổi khác nhau giữa các lần tính
                    .Take(requiredCount)
                    .ToList();

                foreach (var p in bestInPosition)
                {
                    finalLineup.Add(p);
                    selectedIds.Add(p.Id);
                }
            }

            // 2. Dự phòng: Nếu vẫn chưa đủ 7 người (do thiếu vị trí cụ thể)
            // Lấy những người có Quality cao nhất còn lại bất kể vị trí
            if (finalLineup.Count < 7)
            {
                var backups = allPlayers
                    .Where(p => !selectedIds.Contains(p.Id))
                    .OrderByDescending(p => p.Quality)
                    .ThenBy(p => p.Id)
                    .Take(7 - finalLineup.Count)
                    .ToList();

                finalLineup.AddRange(backups);
            }

            return finalLineup.Take(7).ToList();
        }
    }
}
