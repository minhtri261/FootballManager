using FootballManager.Business.DTOs;
using FootballManager.Business.Helpers;
using FootballManager.Business.Repositories;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FootballManager.Business.Services
{
    public class TransferService : BaseService<Transfer>, ITransferService
    {
        private readonly ITransferRepository _transferRepository;
        private readonly IFootballerRepository _footballerRepository;
        private readonly IClubRepository _clubRepository;
        private readonly GameSettings _settings;
        private readonly Random _rand = new Random();
        private readonly ILogger<TransferService> _logger;
        private readonly FootballContext _context;

        public TransferService(ITransferRepository transferRepository, IFootballerRepository footballerRepository, IClubRepository clubRepository, IOptions<GameSettings> settings, FootballContext context, ILogger<TransferService> logger)
            : base(transferRepository)
        {
            _transferRepository = transferRepository;
            _footballerRepository = footballerRepository;
            _clubRepository = clubRepository;
            _settings = settings.Value;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<FootballerDto>> GetListFootballerCanTransferAsync()
        {
            var footballers = await _transferRepository.GetListFootballerCanTransferAsync();
            return footballers.Select(f => new FootballerDto
            {
                Id = f.Id,
                Name = f.Name,
                Age = f.Age,
                Nation = f.Nation,
                Position = f.Position,
                Quality = f.Quality,
                ClubId = f.ClubId,
                ClubName = f.Club != null ? f.Club.Name : null,
                ContractYears = f.ContractYears,
                Status = f.Status,
                AwardQBV = f.AwardQBV,
                AwardQBB = f.AwardQBB,
                AwardQBD = f.AwardQBD,
                IsTransferListed = f.IsTransferListed
            });
        }

        //Lấy danh sách cầu thủ đang được bán trên thị trường chuyển nhượng cho một CLB cụ thể
        public async Task<IEnumerable<Transfer>> GetPendingTransfersForClubAsync(int footballerId)
        {
            return await _transferRepository.GetPendingTransfersForClubAsync(_settings.MyClubId, footballerId);
        }

        //Gửi lời đề nghị chiêu mộ cầu thủ tự do
        public async Task SendOfferToFreeAgentAsync(int clubId, int footballerId, int contractYears)
        {
            await _transferRepository.AddFreeAgentOfferAsync(clubId, footballerId, contractYears);
        }

        public async Task RenewContractAsync(int clubId, int footballerId, int additionalYears)
        {
            await _transferRepository.RenewContractAsync(clubId, footballerId, additionalYears);
        }

        public async Task SendTransferOfferAsync(int fromClubId, int footballerId, int toClubId, int contractYears)
        {
            await _transferRepository.AddTransferOfferAsync(fromClubId, footballerId, toClubId, contractYears);
        }

        //BOT cầu thủ quyết định chấp nhận đề nghị chuyển nhượng
        public async Task BotDecideTransfersAsync()
        {
            _logger.LogInformation("=== BOT: BẮT ĐẦU QUYẾT ĐỊNH TRANSFER ===");
            var pendingTransfers = await _context.Transfers
            .Include(t => t.Footballer)
                .ThenInclude(f => f.Club)
            .Include(t => t.ToClub)
                .ThenInclude(c => c.Footballers)
            .Where(t => t.Status == "Pending")
            .ToListAsync();

            _logger.LogInformation("Số transfer Pending: {Count}", pendingTransfers.Count);
            var soldPositionThisClub = new Dictionary<int, HashSet<string>>(); // ClubId -> set vị trí đã bán

            foreach (var transfer in pendingTransfers)
            {
                var footballer = await _footballerRepository.GetByIdAsync(transfer.FootballerId);
                if (footballer == null) continue;
                _logger.LogInformation("Xử lý cầu thủ {Id} - {Name}", footballer.Id, footballer.Name);
                var clubsInterested = new List<Club>();

                // CLB chủ quản muốn gia hạn
                if (footballer.Club != null)
                {
                    var hasRenewOffer = await _transferRepository.HasRenewOfferAsync(footballer.Id, footballer.ClubId.Value);
                    if (hasRenewOffer) clubsInterested.Add(footballer.Club);
                }


                // Các CLB khác gửi đề nghị
                var otherTransfers = pendingTransfers
                    .Where(t => t.FootballerId == footballer.Id && t.ToClubId != footballer.ClubId)
                    .ToList();

                foreach (var t in otherTransfers)
                {
                    var club = await _clubRepository.GetByIdAsync(t.ToClubId);
                    if (club != null)
                    {
                        clubsInterested.Add(club);
                        _logger.LogInformation("CLB {ClubId} gửi offer cho cầu thủ {Id}", club.Id, footballer.Id);
                    }
                }

                if (clubsInterested.Count == 0)
                {
                    _logger.LogInformation("Không có CLB nào quan tâm cầu thủ {Id} → skip", footballer.Id);
                    continue;
                }

                var clubScores = new Dictionary<Club, double>();
                foreach (var club in clubsInterested.Distinct())
                {
                    // Chỉ áp dụng check vị trí đã bán cho CLB chủ quản đã bán cầu thủ cùng vị trí chưa
                    if (footballer.ClubId != null && club.Id == footballer.ClubId)
                    {
                        var soldSet = soldPositionThisClub.GetValueOrDefault(footballer.ClubId.Value, new HashSet<string>());
                        if (soldSet.Contains(footballer.Position))
                        {
                            _logger.LogInformation("CLB chủ quản {ClubId} đã bán vị trí {Pos} → skip", footballer.ClubId, footballer.Position);
                            continue;
                        }
                    }

                    // Kiểm tra CLB nhận đã đủ 12 cầu thủ chưa
                    var clubPlayerCount = await _clubRepository.CountByClubAsync(club.Id);
                    if (clubPlayerCount >= 12)
                    {
                        _logger.LogInformation("CLB {ClubId} đã đủ 12 cầu thủ → skip", club.Id);
                        continue;
                    }

                    var score = TransferCalculator.CalculateClubScore(footballer, club);
                    clubScores[club] = score;
                    _logger.LogInformation("Score CLB {ClubId} cho cầu thủ {Id}: {Score}", club.Id, footballer.Id, score);
                }

                if (!clubScores.Any())
                {
                    _logger.LogInformation("Không có CLB nào đủ điều kiện để nhận cầu thủ {Id}", footballer.Id);
                    continue;
                }


                var percentages = TransferCalculator.ConvertScoreToPercent(clubScores);
                var selectedClub = TransferCalculator.ChooseClubByRandom(percentages, _rand);
                if (selectedClub == null)
                {
                    _logger.LogWarning("Không chọn được CLB thắng cho cầu thủ {Id}", footballer.Id);
                    continue;
                }
                _logger.LogInformation("CLB {ClubId} được chọn cho cầu thủ {Id}", selectedClub.Id, footballer.Id);
                var winningTransfer = pendingTransfers
                 .FirstOrDefault(t => t.FootballerId == footballer.Id && t.ToClubId == selectedClub.Id);


                if (winningTransfer == null)
                {
                    _logger.LogWarning("Không tìm thấy transfer thắng cho cầu thủ {Id} và CLB {ClubId}", footballer.Id, selectedClub.Id);
                    continue;
                }
                if (winningTransfer.TransferFee > 0)
                {
                    if (selectedClub.Money >= winningTransfer.TransferFee)
                    {
                        selectedClub.Money -= winningTransfer.TransferFee;
                        await _transferRepository.AcceptTransferAsync(winningTransfer, footballer);
                    }
                    else
                    {
                        // không đủ tiền -> mark rejected
                        await _transferRepository.RejectTransferAsync(winningTransfer);
                    }
                }
                else
                {
                    // free agent hoặc renew: accept trực tiếp
                    await _transferRepository.AcceptTransferAsync(winningTransfer, footballer);
                }

                // Đánh dấu vị trí đã bán
                if (footballer.ClubId != null)
                {
                    if (!soldPositionThisClub.ContainsKey(footballer.ClubId.Value))
                        soldPositionThisClub[footballer.ClubId.Value] = new HashSet<string>();
                    soldPositionThisClub[footballer.ClubId.Value].Add(footballer.Position);
                }

                // Các transfer thua mark Rejected
                var losingTransfers = pendingTransfers
                    .Where(t => t.FootballerId == footballer.Id && t.Id != winningTransfer.Id)
                    .ToList();
                foreach (var lt in losingTransfers)
                {
                    await _transferRepository.RejectTransferAsync(lt);
                }
            }

            // Kiểm tra các CLB BOT sau khi hết lượt
            var allClubs = await _clubRepository.GetAllAsync();
            foreach (var club in allClubs.Where(c => c.IsBot)) // trừ CLB người chơi nếu muốn
            {
                var playerCount = (await _clubRepository.CountByClubAsync(club.Id));
                if (playerCount >= 9)
                {
                    await _clubRepository.SetClubFinalizedAsync(club.Id, true);
                }
            }
        }

        //BOT CLB quyết định mua hoặc gia hạn cầu thủ
        public async Task BotDecideWhoToBuyOrRenewAsync()
        {
            var rng = new Random();

            _logger.LogInformation("=== BOT: BẮT ĐẦU BOT DECISION ===");
            var allClubs = (await _clubRepository.GetAllAsync())
                .Where(c => c.IsBot && !c.IsFinalized && c.Id != 3)
                .ToList();

            _logger.LogInformation("Số CLB BOT cần xử lý: {Count}", allClubs.Count);
            var allPlayers = await _footballerRepository.GetAllAsync();

            var transferablePlayers = allPlayers
            .Where(f => f.IsTransferListed && (f.ClubId == null || !f.Club.IsFinalized))
            .ToList();

            _logger.LogInformation("Số cầu thủ đang transfer listed: {Count}", transferablePlayers.Count);
            // Pre-calc số cầu thủ theo CLB và theo vị trí
            _logger.LogInformation("Đang tính toán clubPlayerCount...");
            var clubPlayerCount = allPlayers
            .Where(p => p.ClubId.HasValue)
            .GroupBy(p => p.ClubId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

            _logger.LogInformation("Đang tính toán clubPositionCount...");
            var clubPositionCount = allPlayers
            .Where(p => p.ClubId.HasValue)
            .GroupBy(p => new { p.ClubId, p.Position })
            .ToDictionary(g => (g.Key.ClubId.Value, g.Key.Position), g => g.Count());

            var offersToAdd = new List<Transfer>();
            var updatedPlayers = new List<Footballer>();

            foreach (var club in allClubs)
            {
                var offeredPlayerIdsThisClub = new HashSet<int>();
                _logger.LogInformation("---- CLB {ClubId} ({Name}) bắt đầu xử lý ----", club.Id, club.Name);
                var clubBudget = club.Money * 0.5m; // BOT chỉ dùng tối đa 50% tiền
                _logger.LogInformation("CLB {ClubId} ngân sách dùng để mua/gia hạn: {Budget}", club.Id, clubBudget);
                var currentPlayers = allPlayers.Where(p => p.ClubId == club.Id).ToList();
                _logger.LogInformation("CLB {ClubId} hiện có {Count} cầu thủ", club.Id, currentPlayers.Count);

                var positionCounts = new Dictionary<string, int>
                {
                    ["GK"] = currentPlayers.Count(f => f.Position == "GK"),
                    ["CB"] = currentPlayers.Count(f => f.Position == "CB"),
                    ["Mid"] = currentPlayers.Count(f => f.Position == "DM" || f.Position == "CM" || f.Position == "AM"),
                    ["CF"] = currentPlayers.Count(f => f.Position == "CF")
                };

                var targetCounts = new Dictionary<string, int>
                {
                    ["GK"] = 2,
                    ["CB"] = 3,
                    ["Mid"] = 4,
                    ["CF"] = 3
                };

                var boughtFromClubThisRound = new Dictionary<int, int>(); // CLB -> số cầu thủ đã mua
                int offersSent = 0;

                foreach (var pos in targetCounts.Keys)
                {
                    if (offersSent >= 5)
                    {
                        _logger.LogInformation("CLB {ClubId} đã gửi đủ số offer test (5). Dừng vòng position.", club.Id);
                        break;
                    }

                    _logger.LogInformation("→ Xử lý vị trí {Pos}", pos);

                    int loopGuard = 0;
                    while (positionCounts[pos] < targetCounts[pos])
                    {
                        loopGuard++;
                        if (loopGuard > 200) // threshold an toàn (tùy chỉnh)
                        {
                            _logger.LogWarning("POSSIBLE INFINITE LOOP at club {ClubId} pos {Pos} - breaking after {Count} iterations", club.Id, pos, loopGuard);
                            break;
                        }

                        _logger.LogInformation("Còn thiếu {Missing} cầu thủ ở vị trí {Pos}",
                    targetCounts[pos] - positionCounts[pos], pos);

                        if (currentPlayers.Count >= 12)
                        {
                            _logger.LogInformation("CLB {ClubId} đã có 12 cầu thủ → dừng mua/gia hạn.", club.Id);
                            break;
                        }

                        if (offersSent >= 5)
                        {
                            _logger.LogInformation("CLB {ClubId} đã gửi 5 offer → dừng.", club.Id);
                            break;
                        }

                        // Ưu tiên gia hạn cầu thủ đang có hợp đồng <= 1 năm
                        var candidatesToRenew = currentPlayers
                            .Where(f => (pos == "Mid"
                                         ? (f.Position == "DM" || f.Position == "CM" || f.Position == "AM")
                                         : f.Position == pos)
                                        && f.ContractYears <= 1)
                            .OrderByDescending(f => f.Quality)
                            .ToList();

                        if (candidatesToRenew.Any())
                        {
                            var best = candidatesToRenew.First();
                            var renewCost = (decimal)best.Quality;
                            _logger.LogInformation("Gia hạn cầu thủ {Id} ({Name}) với cost {Cost}",
                        best.Id, best.Name, renewCost);
                            if (renewCost <= clubBudget) // chỉ gia hạn nếu đủ ngân sách
                            {
                                await _transferRepository.RenewContractAsync(club.Id, best.Id, 1);
                                clubBudget -= renewCost;

                                positionCounts[pos]++;
                                currentPlayers.RemoveAll(p => p.Id == best.Id);
                                _logger.LogInformation("→ Gia hạn thành công. ClubBudget: {Budget}", clubBudget);
                            }
                            continue;
                        }

                        _logger.LogInformation("Không có ai để gia hạn → tìm cầu thủ Transfer Listed cho vị trí {Pos}", pos);
                        // Nếu không có cầu thủ cần gia hạn -> tìm cầu thủ trên thị trường
                        var availableFootballers = transferablePlayers
                       .Where(f => f.ClubId != club.Id &&
                                   (pos == "Mid"
                                       ? (f.Position == "DM" || f.Position == "CM" || f.Position == "AM")
                                       : f.Position == pos))
                       .OrderByDescending(f => f.Quality)
                       .ThenBy(f => f.Age)
                       .ToList();

                        _logger.LogInformation("Tìm thấy {Count} cầu thủ phù hợp ở TTCN", availableFootballers.Count);
                        if (!availableFootballers.Any()) break;

                        foreach (var candidate in availableFootballers)
                        {
                            _logger.LogInformation("→ Thử cầu thủ {Id} - {Name}", candidate.Id, candidate.Name);
                            // Nếu cầu thủ có CLB nguồn, kiểm tra điều kiện bảo vệ CLB nguồn
                            if (candidate.ClubId != null)
                            {
                                int playerCountInSource = clubPlayerCount.GetValueOrDefault(candidate.ClubId.Value, 0);

                                if (playerCountInSource <= 7)
                                {
                                    _logger.LogInformation("CLB nguồn {ClubId} <8 cầu thủ → bỏ qua", candidate.ClubId.Value);
                                    continue;
                                }

                                if (candidate.Position == "GK")
                                {
                                    int gkCountInSource = clubPositionCount.GetValueOrDefault((candidate.ClubId.Value, "GK"), 0);
                                    if (gkCountInSource <= 1)
                                    {
                                        _logger.LogInformation("CLB nguồn {ClubId} chỉ còn 1 GK → bỏ qua", candidate.ClubId.Value);
                                        continue;
                                    }
                                }

                                // giá nếu có CLB nguồn
                                var price = TransferHelper.CalculateTransferPrice(candidate);
                                if (price > clubBudget)
                                {
                                    _logger.LogInformation("Giá {Price} > ngân sách {Budget} → bỏ qua", price, clubBudget);
                                    continue;
                                }

                                // BOT chỉ gửi tối đa 2 offer tới cùng 1 CLB
                                if (boughtFromClubThisRound.GetValueOrDefault(candidate.ClubId.Value, 0) >= 2)
                                {
                                    _logger.LogInformation("CLB nguồn {ClubId} đã bán 2 → bỏ qua", candidate.ClubId.Value);
                                    continue;
                                }
                            }
                            else
                            {
                                // free agent: price = 0
                            }

                            // Xác suất mua (áp dụng cho cả free agent và có CLB)
                            var avgQuality = currentPlayers.Any() ? currentPlayers.Average(f => f.Quality) : candidate.Quality;
                            double qualityFactor = candidate.Quality / (avgQuality + 0.01);
                            double ageFactor = candidate.Age <= 30 ? 1 : Math.Max(0.5, 1 - (candidate.Age - 30) * 0.05);
                            double acceptanceProbability = Math.Min(1, qualityFactor * ageFactor);

                            double roll = rng.NextDouble();
                            _logger.LogInformation("Xác suất nhận offer = {Prob}, roll = {Roll}", acceptanceProbability, roll);

                            if (roll <= acceptanceProbability)
                            {
                                // ⛔ Nếu đã gửi offer cho cầu thủ này trong lượt này → bỏ qua
                                if (offeredPlayerIdsThisClub.Contains(candidate.Id))
                                {
                                    _logger.LogInformation("CLB {ClubId} đã gửi offer cho {PlayerId} rồi → bỏ qua", club.Id, candidate.Id);
                                    break;
                                }

                                // ghi nhận đã gửi offer
                                offeredPlayerIdsThisClub.Add(candidate.Id);

                                // gửi offer (free agent hoặc từ CLB khác)
                                if (candidate.ClubId == null)
                                {
                                    await _transferRepository.AddFreeAgentOfferAsync(club.Id, candidate.Id, 2);
                                }
                                else
                                {
                                    var price = TransferHelper.CalculateTransferPrice(candidate);
                                    await _transferRepository.AddTransferOfferAsync(candidate.ClubId.Value, candidate.Id, club.Id, 2);
                                    boughtFromClubThisRound[candidate.ClubId.Value] =
                                        boughtFromClubThisRound.GetValueOrDefault(candidate.ClubId.Value, 0) + 1;
                                    clubBudget -= price;
                                }

                                // update cache
                                positionCounts[pos]++;
                                offersSent++;

                                _logger.LogInformation("Gửi offer thành công cho {Id}", candidate.Id);
                                break; // đã gửi 1 offer cho vị trí này -> thoát candidate loop
                            }

                            break; // tránh vòng vô tận
                        }
                    }
                    _logger.LogInformation("---- CLB {ClubId} xử lý xong (Sent {Offers}) ----",
               club.Id, offersSent);
                }
            }
        }
    } 
}
