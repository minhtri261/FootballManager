using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
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

        public TransferService(
            ITransferRepository transferRepository,
            IFootballerRepository footballerRepository,
            IClubRepository clubRepository,
            IOptions<GameSettings> settings,
            IUnitOfWork unitOfWork,
            ILogger<TransferService> logger)
            : base(transferRepository, unitOfWork)
        {
            _transferRepository = transferRepository;
            _footballerRepository = footballerRepository;
            _clubRepository = clubRepository;
            _settings = settings.Value;
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
                Position = f.Position.ToString(),
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

        // Lấy danh sách yêu cầu chuyển nhượng đang chờ xử lý cho CLB
        public async Task<IEnumerable<Transfer>> GetPendingTransfersForClubAsync(int footballerId)
        {
            return await _transferRepository.GetPendingTransfersForClubAsync(_settings.MyClubId, footballerId);
        }

        // Gửi lời đề nghị chiêu mộ cầu thủ tự do
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

        private static bool MatchesPositionGroup(PlayerPosition position, string group) => group switch
        {
            "GK" => position == PlayerPosition.GK,
            "CB" => position == PlayerPosition.CB,
            "Mid" => position is PlayerPosition.DM or PlayerPosition.CM or PlayerPosition.AM,
            "CF" => position == PlayerPosition.ST,
            _ => false
        };

        // BOT cầu thủ quyết định chấp nhận đề nghị chuyển nhượng
        public async Task BotDecideTransfersAsync()
        {
            _logger.LogInformation("=== BOT: BẮT ĐẦU QUYẾT ĐỊNH TRANSFER ===");
            var pendingTransfers = await _transferRepository.GetPendingTransfersWithDetailsAsync();

            _logger.LogInformation("Số transfer Pending: {Count}", pendingTransfers.Count);
            var soldPositionThisClub = new Dictionary<int, HashSet<PlayerPosition>>();

            foreach (var transfer in pendingTransfers)
            {
                var footballer = await _footballerRepository.GetByIdAsync(transfer.FootballerId);
                if (footballer == null) continue;
                _logger.LogInformation("Xử lý cầu thủ {Id} - {Name}", footballer.Id, footballer.Name);
                var clubsInterested = new List<Club>();

                if (footballer.Club != null)
                {
                    var hasRenewOffer = await _transferRepository.HasRenewOfferAsync(footballer.Id, footballer.ClubId!.Value);
                    if (hasRenewOffer) clubsInterested.Add(footballer.Club);
                }

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
                    if (footballer.ClubId != null && club.Id == footballer.ClubId)
                    {
                        var soldSet = soldPositionThisClub.GetValueOrDefault(footballer.ClubId.Value, new HashSet<PlayerPosition>());
                        if (soldSet.Contains(footballer.Position))
                        {
                            _logger.LogInformation("CLB chủ quản {ClubId} đã bán vị trí {Pos} → skip", footballer.ClubId, footballer.Position);
                            continue;
                        }
                    }

                    var clubPlayerCount = await _footballerRepository.CountByClubAsync(club.Id);
                    if (clubPlayerCount >= 12)
                    {
                        _logger.LogInformation("CLB {ClubId} đã đủ 12 cầu thủ → skip", club.Id);
                        continue;
                    }

                    var score = Helpers.TransferCalculator.CalculateClubScore(footballer, club);
                    clubScores[club] = score;
                    _logger.LogInformation("Score CLB {ClubId} cho cầu thủ {Id}: {Score}", club.Id, footballer.Id, score);
                }

                if (!clubScores.Any())
                {
                    _logger.LogInformation("Không có CLB nào đủ điều kiện để nhận cầu thủ {Id}", footballer.Id);
                    continue;
                }

                var percentages = Helpers.TransferCalculator.ConvertScoreToPercent(clubScores);
                var selectedClub = Helpers.TransferCalculator.ChooseClubByRandom(percentages, _rand);
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
                        await _transferRepository.RejectTransferAsync(winningTransfer);
                    }
                }
                else
                {
                    await _transferRepository.AcceptTransferAsync(winningTransfer, footballer);
                }

                if (footballer.ClubId != null)
                {
                    if (!soldPositionThisClub.ContainsKey(footballer.ClubId.Value))
                        soldPositionThisClub[footballer.ClubId.Value] = new HashSet<PlayerPosition>();
                    soldPositionThisClub[footballer.ClubId.Value].Add(footballer.Position);
                }

                var losingTransfers = pendingTransfers
                    .Where(t => t.FootballerId == footballer.Id && t.Id != winningTransfer.Id)
                    .ToList();
                foreach (var lt in losingTransfers)
                {
                    await _transferRepository.RejectTransferAsync(lt);
                }
            }

            // Kiểm tra các CLB BOT sau khi hết lượt, đánh dấu đã đủ quân số
            var allClubs = await _clubRepository.GetAll().ToListAsync();
            foreach (var club in allClubs.Where(c => c.IsBot))
            {
                var playerCount = await _footballerRepository.CountByClubAsync(club.Id);
                if (playerCount >= 9)
                {
                    club.IsFinalized = true;
                    _clubRepository.Update(club);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        // BOT CLB quyết định mua hoặc gia hạn cầu thủ
        public async Task BotDecideWhoToBuyOrRenewAsync()
        {
            var rng = new Random();

            _logger.LogInformation("=== BOT: BẮT ĐẦU BOT DECISION ===");
            var allClubs = (await _clubRepository.GetAll().ToListAsync())
                .Where(c => c.IsBot && !c.IsFinalized && c.Id != _settings.MyClubId)
                .ToList();

            _logger.LogInformation("Số CLB BOT cần xử lý: {Count}", allClubs.Count);
            var allPlayers = await _footballerRepository.GetAll().ToListAsync();

            var transferablePlayers = allPlayers
                .Where(f => f.IsTransferListed && (f.ClubId == null || !f.Club!.IsFinalized))
                .ToList();

            _logger.LogInformation("Số cầu thủ đang transfer listed: {Count}", transferablePlayers.Count);

            var clubPlayerCount = allPlayers
                .Where(p => p.ClubId.HasValue)
                .GroupBy(p => p.ClubId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var clubPositionCount = allPlayers
                .Where(p => p.ClubId.HasValue)
                .GroupBy(p => new { p.ClubId, p.Position })
                .ToDictionary(g => (g.Key.ClubId!.Value, g.Key.Position), g => g.Count());

            foreach (var club in allClubs)
            {
                var offeredPlayerIdsThisClub = new HashSet<int>();
                _logger.LogInformation("---- CLB {ClubId} ({Name}) bắt đầu xử lý ----", club.Id, club.Name);
                var clubBudget = club.Money * 0.5m;
                _logger.LogInformation("CLB {ClubId} ngân sách dùng để mua/gia hạn: {Budget}", club.Id, clubBudget);
                var currentPlayers = allPlayers.Where(p => p.ClubId == club.Id).ToList();
                _logger.LogInformation("CLB {ClubId} hiện có {Count} cầu thủ", club.Id, currentPlayers.Count);

                var positionCounts = new Dictionary<string, int>
                {
                    ["GK"] = currentPlayers.Count(f => f.Position == PlayerPosition.GK),
                    ["CB"] = currentPlayers.Count(f => f.Position == PlayerPosition.CB),
                    ["Mid"] = currentPlayers.Count(f => f.Position is PlayerPosition.DM or PlayerPosition.CM or PlayerPosition.AM),
                    ["CF"] = currentPlayers.Count(f => f.Position == PlayerPosition.ST)
                };

                var targetCounts = new Dictionary<string, int>
                {
                    ["GK"] = 2,
                    ["CB"] = 3,
                    ["Mid"] = 4,
                    ["CF"] = 3
                };

                var boughtFromClubThisRound = new Dictionary<int, int>();
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
                        if (loopGuard > 200)
                        {
                            _logger.LogWarning("POSSIBLE INFINITE LOOP at club {ClubId} pos {Pos} - breaking after {Count} iterations", club.Id, pos, loopGuard);
                            break;
                        }

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
                            .Where(f => MatchesPositionGroup(f.Position, pos) && f.ContractYears <= 1)
                            .OrderByDescending(f => f.Quality)
                            .ToList();

                        if (candidatesToRenew.Any())
                        {
                            var best = candidatesToRenew.First();
                            var renewCost = (decimal)best.Quality;
                            _logger.LogInformation("Gia hạn cầu thủ {Id} ({Name}) với cost {Cost}", best.Id, best.Name, renewCost);
                            if (renewCost <= clubBudget)
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
                        var availableFootballers = transferablePlayers
                            .Where(f => f.ClubId != club.Id && MatchesPositionGroup(f.Position, pos))
                            .OrderByDescending(f => f.Quality)
                            .ThenBy(f => f.Age)
                            .ToList();

                        _logger.LogInformation("Tìm thấy {Count} cầu thủ phù hợp ở TTCN", availableFootballers.Count);
                        if (!availableFootballers.Any()) break;

                        foreach (var candidate in availableFootballers)
                        {
                            _logger.LogInformation("→ Thử cầu thủ {Id} - {Name}", candidate.Id, candidate.Name);
                            if (candidate.ClubId != null)
                            {
                                int playerCountInSource = clubPlayerCount.GetValueOrDefault(candidate.ClubId.Value, 0);

                                if (playerCountInSource <= 7)
                                {
                                    _logger.LogInformation("CLB nguồn {ClubId} <8 cầu thủ → bỏ qua", candidate.ClubId.Value);
                                    continue;
                                }

                                if (candidate.Position == PlayerPosition.GK)
                                {
                                    int gkCountInSource = clubPositionCount.GetValueOrDefault((candidate.ClubId.Value, PlayerPosition.GK), 0);
                                    if (gkCountInSource <= 1)
                                    {
                                        _logger.LogInformation("CLB nguồn {ClubId} chỉ còn 1 GK → bỏ qua", candidate.ClubId.Value);
                                        continue;
                                    }
                                }

                                var price = CalculateTransferPriceForBudget(candidate);
                                if (price > clubBudget)
                                {
                                    _logger.LogInformation("Giá {Price} > ngân sách {Budget} → bỏ qua", price, clubBudget);
                                    continue;
                                }

                                if (boughtFromClubThisRound.GetValueOrDefault(candidate.ClubId.Value, 0) >= 2)
                                {
                                    _logger.LogInformation("CLB nguồn {ClubId} đã bán 2 → bỏ qua", candidate.ClubId.Value);
                                    continue;
                                }
                            }

                            var avgQuality = currentPlayers.Any() ? currentPlayers.Average(f => f.Quality) : candidate.Quality;
                            double qualityFactor = candidate.Quality / (avgQuality + 0.01);
                            double ageFactor = candidate.Age <= 30 ? 1 : Math.Max(0.5, 1 - (candidate.Age - 30) * 0.05);
                            double acceptanceProbability = Math.Min(1, qualityFactor * ageFactor);

                            double roll = rng.NextDouble();
                            _logger.LogInformation("Xác suất nhận offer = {Prob}, roll = {Roll}", acceptanceProbability, roll);

                            if (roll <= acceptanceProbability)
                            {
                                if (offeredPlayerIdsThisClub.Contains(candidate.Id))
                                {
                                    _logger.LogInformation("CLB {ClubId} đã gửi offer cho {PlayerId} rồi → bỏ qua", club.Id, candidate.Id);
                                    break;
                                }

                                offeredPlayerIdsThisClub.Add(candidate.Id);

                                if (candidate.ClubId == null)
                                {
                                    await _transferRepository.AddFreeAgentOfferAsync(club.Id, candidate.Id, 2);
                                }
                                else
                                {
                                    var price = CalculateTransferPriceForBudget(candidate);
                                    await _transferRepository.AddTransferOfferAsync(candidate.ClubId.Value, candidate.Id, club.Id, 2);
                                    boughtFromClubThisRound[candidate.ClubId.Value] =
                                        boughtFromClubThisRound.GetValueOrDefault(candidate.ClubId.Value, 0) + 1;
                                    clubBudget -= price;
                                }

                                positionCounts[pos]++;
                                offersSent++;

                                _logger.LogInformation("Gửi offer thành công cho {Id}", candidate.Id);
                                break;
                            }

                            break;
                        }
                    }
                    _logger.LogInformation("---- CLB {ClubId} xử lý xong (Sent {Offers}) ----", club.Id, offersSent);
                }
            }
        }

        private static decimal CalculateTransferPriceForBudget(Footballer player) => player.ContractYears * player.Quality;
    }
}
