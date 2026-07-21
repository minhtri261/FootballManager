using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FootballManager.Business.Services
{
    public class SeasonService : ISeasonService
    {
        // C1: Round 1-3 = vòng bảng (2 bảng x 4 đội, đá 1 lượt), Round 4 = Bán kết, Round 5 = Chung kết
        private const int C1GroupStageRounds = 3;
        private const int C1SemifinalRound = C1GroupStageRounds + 1;

        private readonly ITournamentRepository _tournamentRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IMatchRepository _matchRepo;
        private readonly IGameStateRepository _gameStateRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SeasonService> _logger;

        public SeasonService(
            ITournamentRepository tournamentRepo,
            IClubRepository clubRepo,
            IMatchRepository matchRepo,
            IGameStateRepository gameStateRepo,
            IUnitOfWork unitOfWork,
            ILogger<SeasonService> logger)
        {
            _tournamentRepo = tournamentRepo;
            _clubRepo = clubRepo;
            _matchRepo = matchRepo;
            _gameStateRepo = gameStateRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task InitializeSeasonAsync(int seasonNumber)
        {
            var tournaments = await _tournamentRepo.GetTournamentsBySeasonAsync(seasonNumber);

            if (!tournaments.Any() && seasonNumber > 1)
            {
                tournaments = await CloneTournamentsFromPreviousSeasonAsync(seasonNumber);
            }

            if (!tournaments.Any())
            {
                _logger.LogWarning("Không tìm thấy Tournament nào cho mùa {Season} để khởi tạo.", seasonNumber);
                return;
            }

            // seedRanksByNation["Vietnam"] = danh sách ClubId theo thứ tự seed (đầu = mạnh nhất)
            var seedRanksByNation = new Dictionary<string, List<int>>();

            foreach (var tournament in tournaments.Where(t => t.Nation != null))
            {
                await AssignNationalClubsAsync(tournament, seasonNumber, seedRanksByNation);
            }
            await _unitOfWork.SaveChangesAsync(); // flush trước khi đọc lại standings cho bước tiếp theo

            // Chỉ giải châu lục (C1) mới gom CLB kiểu "top-N mỗi quốc gia". Các giải khác có Nation=null
            // (chưa có dữ liệu Club cho quốc gia đó) thì bỏ qua hẳn — để trống như yêu cầu, không gán nhầm CLB.
            foreach (var tournament in tournaments.Where(t => t.Type == TournamentType.C1))
            {
                await AssignContinentalClubsAsync(tournament, seedRanksByNation);
            }
            await _unitOfWork.SaveChangesAsync();

            var templates = await _gameStateRepo.GetAllTemplatesAsync();

            foreach (var tournament in tournaments.Where(t => t.Type == TournamentType.League))
            {
                await GenerateLeagueFixturesAsync(tournament, seasonNumber, templates);
            }

            foreach (var tournament in tournaments.Where(t => t.Type == TournamentType.Cup))
            {
                await GenerateKnockoutRoundOneAsync(tournament, seasonNumber, templates);
            }

            foreach (var tournament in tournaments.Where(t => t.Type == TournamentType.C1))
            {
                await GenerateGroupStageFixturesAsync(tournament, seasonNumber, templates);
            }
        }

        public async Task EnsureRoundReadyAsync(TournamentType type, int round, int seasonNumber)
        {
            if (type == TournamentType.League) return; // đã sinh hết ở InitializeSeasonAsync
            if (type == TournamentType.Cup && round <= 1) return; // Round 1 đã sinh ở InitializeSeasonAsync
            if (type == TournamentType.C1 && round <= C1GroupStageRounds) return; // vòng bảng đã sinh hết ở InitializeSeasonAsync

            var tournaments = (await _tournamentRepo.GetTournamentsBySeasonAsync(seasonNumber))
                .Where(t => t.Type == type)
                .ToList();
            if (!tournaments.Any()) return;

            var templates = await _gameStateRepo.GetAllTemplatesAsync();
            var week = templates.FirstOrDefault(t => t.TournamentType == type && t.Round == round)?.Week;

            foreach (var tournament in tournaments)
            {
                var existing = await _matchRepo.GetMatchesByTournamentAndRoundAsync(tournament.Id, round);
                if (existing.Any()) continue; // idempotent

                if (week == null)
                {
                    _logger.LogWarning("Không tìm thấy ScheduleTemplate cho {Type} Round {Round} — bỏ qua sinh lịch {Name}.", type, round, tournament.Name);
                    continue;
                }

                if (type == TournamentType.C1 && round == C1SemifinalRound)
                {
                    await GenerateC1SemifinalFromGroupsAsync(tournament, seasonNumber, week.Value, round);
                }
                else
                {
                    await GenerateKnockoutNextRoundAsync(tournament, seasonNumber, round, week.Value);
                }
            }
        }

        public async Task<List<int>> GetFinalRankingAsync(Tournament tournament, int seasonNumber)
        {
            var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id); // Points/GD/GF giảm dần

            // League: BXH chính là thứ hạng cuối cùng.
            if (tournament.Type == TournamentType.League || !standings.Any())
                return standings.Select(tc => tc.ClubId).ToList();

            // Cup/C1: BXH theo Points chỉ là "trùng hợp thường đúng" với knockout, không đảm bảo — lấy
            // thứ hạng thực tế từ kết quả bracket (ai thắng Chung kết mới là vô địch).
            var matches = await _matchRepo.GetMatchesByTournamentAsync(tournament.Id);
            int maxRound = matches.Any() ? matches.Max(m => m.Round) : 0;
            var finalMatch = matches.FirstOrDefault(m => m.Round == maxRound && m.IsPlayed);
            if (finalMatch == null)
                return standings.Select(tc => tc.ClubId).ToList(); // chưa đá xong chung kết -> fallback theo Points

            var rankByClub = standings.ToDictionary(tc => tc.ClubId, tc => tc.Rank);
            var ranking = new List<int>();

            int champion = DetermineWinner(finalMatch, rankByClub);
            int runnerUp = champion == finalMatch.HomeClubId ? (finalMatch.AwayClubId ?? finalMatch.HomeClubId) : finalMatch.HomeClubId;
            ranking.Add(champion);
            if (runnerUp != champion) ranking.Add(runnerUp);

            if (maxRound - 1 >= 1)
            {
                var semiLosers = matches.Where(m => m.Round == maxRound - 1 && m.IsPlayed)
                    .Select(m =>
                    {
                        int w = DetermineWinner(m, rankByClub);
                        return w == m.HomeClubId ? (m.AwayClubId ?? m.HomeClubId) : m.HomeClubId;
                    })
                    .Where(id => !ranking.Contains(id))
                    .Distinct()
                    .ToList();

                ranking.AddRange(standings.Select(tc => tc.ClubId).Where(id => semiLosers.Contains(id)));
            }

            // Các CLB còn lại (bị loại sớm hơn — chỉ có ở C1 vòng bảng) -> sắp theo Points/GD
            ranking.AddRange(standings.Select(tc => tc.ClubId).Where(id => !ranking.Contains(id)));

            return ranking;
        }

        public async Task<int?> FinalizeSeasonAwardsAsync(int seasonNumber)
        {
            var tournaments = await _tournamentRepo.GetTournamentsBySeasonAsync(seasonNumber);
            var mvpCounts = await _matchRepo.GetMvpCountsBySeasonAsync(seasonNumber);
            var clubAchievementBonus = new Dictionary<int, int>();
            var summaryRepo = _unitOfWork.Repository<SeasonSummary>();

            foreach (var tournament in tournaments)
            {
                var ranking = await GetFinalRankingAsync(tournament, seasonNumber);
                if (!ranking.Any()) continue;

                for (int i = 0; i < ranking.Count; i++)
                {
                    int bonus = i switch { 0 => 30, 1 => 15, 2 => 8, 3 => 4, _ => 0 };
                    if (bonus == 0) continue;
                    clubAchievementBonus[ranking[i]] = clubAchievementBonus.GetValueOrDefault(ranking[i]) + bonus;
                }

                await summaryRepo.AddAsync(new SeasonSummary
                {
                    SeasonNumber = seasonNumber,
                    TournamentId = tournament.Id,
                    ChampionClubId = ranking[0],
                    TopScorerId = await _matchRepo.GetTopScorerAsync(tournament.Id, seasonNumber),
                    MVPFootballerId = await _matchRepo.GetTournamentMvpAsync(tournament.Id, seasonNumber)
                });
            }

            // QBV/QBB/QBĐ: top 3 cầu thủ theo score = Quality + thành tích CLB (cộng dồn mọi giải) + số lần MVP*5
            var footballerRepo = _unitOfWork.Repository<Footballer>();
            var candidates = await footballerRepo.GetAll().Where(f => f.ClubId != null).ToListAsync();

            var ranked = candidates
                .Select(f => new
                {
                    Footballer = f,
                    Score = f.Quality
                        + clubAchievementBonus.GetValueOrDefault(f.ClubId!.Value)
                        + mvpCounts.GetValueOrDefault(f.Id) * 5
                })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                if (i == 0) ranked[i].Footballer.AwardQBV++;
                else if (i == 1) ranked[i].Footballer.AwardQBB++;
                else ranked[i].Footballer.AwardQBD++;
                footballerRepo.Update(ranked[i].Footballer);
            }

            await _unitOfWork.SaveChangesAsync();

            return ranked.Count > 0 ? ranked[0].Footballer.Id : null;
        }

        // Mốc tuổi phân loại PlayerLifeCycle
        private static PlayerLifeCycle ClassifyLifeCycle(int age) => age switch
        {
            <= 20 => PlayerLifeCycle.Youth,
            <= 24 => PlayerLifeCycle.Rising,
            <= 29 => PlayerLifeCycle.Peak,
            <= 32 => PlayerLifeCycle.Stable,
            _ => PlayerLifeCycle.Veteran
        };

        // Trọng số được chọn nhận điểm training của CLB, theo PlayerLifeCycle (Veteran không nằm trong pool)
        private static readonly Dictionary<PlayerLifeCycle, double> TrainingWeight = new()
        {
            [PlayerLifeCycle.Youth] = 4.0,
            [PlayerLifeCycle.Rising] = 3.0,
            [PlayerLifeCycle.Peak] = 2.0,
            [PlayerLifeCycle.Stable] = 1.0,
        };

        public async Task ApplyPlayerDevelopmentAsync(int? qbvWinnerFootballerId)
        {
            var footballerRepo = _unitOfWork.Repository<Footballer>();
            var players = await footballerRepo.GetAll().Where(f => f.Status != PlayerLifeCycle.Retired).ToListAsync();
            var trainingPoolByClub = new Dictionary<int, List<Footballer>>();
            var rand = Random.Shared;

            foreach (var player in players)
            {
                player.Age += 1;
                player.Status = ClassifyLifeCycle(player.Age);

                if (qbvWinnerFootballerId.HasValue && player.Id == qbvWinnerFootballerId.Value)
                    player.Quality = Math.Min(player.Quality + 1, player.Potential);

                if (player.Status == PlayerLifeCycle.Veteran)
                {
                    int decline = 1 + (player.Age - 33) / 3;
                    if (player.Quality - decline <= 0)
                    {
                        player.Quality = 0;
                        player.Status = PlayerLifeCycle.Retired;
                        player.ClubId = null;
                    }
                    else
                    {
                        player.Quality -= decline;
                    }
                }
                else if (player.ClubId.HasValue)
                {
                    if (!trainingPoolByClub.TryGetValue(player.ClubId.Value, out var pool))
                    {
                        pool = new List<Footballer>();
                        trainingPoolByClub[player.ClubId.Value] = pool;
                    }
                    pool.Add(player);
                }

                footballerRepo.Update(player);
            }

            var clubs = await _clubRepo.GetAll().Where(c => c.TrainingQuality > 0).ToListAsync();
            foreach (var club in clubs)
            {
                if (!trainingPoolByClub.TryGetValue(club.Id, out var pool) || !pool.Any()) continue;

                for (int i = 0; i < club.TrainingQuality; i++)
                {
                    var eligible = pool.Where(p => p.Quality < p.Potential).ToList();
                    if (!eligible.Any()) break; // cả đội đã đạt Potential, không còn ai để train

                    var pick = WeightedPickByLifeCycle(eligible, rand);
                    pick.Quality = Math.Min(pick.Quality + 1, pick.Potential);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private static Footballer WeightedPickByLifeCycle(List<Footballer> candidates, Random rand)
        {
            double total = candidates.Sum(p => TrainingWeight.GetValueOrDefault(p.Status, 0.5));
            double roll = rand.NextDouble() * total;
            double cumulative = 0;
            foreach (var p in candidates)
            {
                cumulative += TrainingWeight.GetValueOrDefault(p.Status, 0.5);
                if (roll <= cumulative) return p;
            }
            return candidates[^1];
        }

        // Độ lệch Quality so với Club.YouthTrainingQuality khi sinh cầu thủ trẻ mới, kèm trọng số (%) — luôn kẹp tối thiểu 1
        private static readonly (int Delta, double Weight)[] YouthQualityDeltas =
        {
            (0, 75), (1, 12), (-1, 6), (2, 4), (-2, 2), (3, 1)
        };

        // Quân số tối đa 1 CLB được giữ — khớp khung mục tiêu của Bot (GK2+CB3+Mid4+CF3=12), dư 2 chỗ làm chiều sâu đội hình
        private const int MaxSquadSize = 14;

        public async Task PromoteYouthPlayersAsync(int seasonNumber)
        {
            var clubs = await _clubRepo.GetAll().ToListAsync();
            var footballerRepo = _unitOfWork.Repository<Footballer>();
            var squadSizeByClub = (await footballerRepo.GetAll().Where(f => f.ClubId != null).ToListAsync())
                .GroupBy(f => f.ClubId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            var rand = Random.Shared;

            var surnamesByNation = new Dictionary<string, List<string>>();
            var givenNamesByNation = new Dictionary<string, List<string>>();

            foreach (var club in clubs)
            {
                if (string.IsNullOrEmpty(club.Nation)) continue;

                if (squadSizeByClub.GetValueOrDefault(club.Id) >= MaxSquadSize)
                {
                    _logger.LogInformation("CLB {Club} đã đủ {Max} cầu thủ — bỏ qua đôn trẻ mùa này.", club.Name, MaxSquadSize);
                    continue;
                }

                if (!surnamesByNation.TryGetValue(club.Nation, out var surnames))
                {
                    surnames = await _unitOfWork.Repository<PlayerSurname>().GetAll()
                        .Where(s => s.Nation == club.Nation).Select(s => s.Name).ToListAsync();
                    surnamesByNation[club.Nation] = surnames;
                }
                if (!givenNamesByNation.TryGetValue(club.Nation, out var givenNames))
                {
                    givenNames = await _unitOfWork.Repository<PlayerGivenName>().GetAll()
                        .Where(g => g.Nation == club.Nation).Select(g => g.Name).ToListAsync();
                    givenNamesByNation[club.Nation] = givenNames;
                }

                if (!surnames.Any() || !givenNames.Any())
                {
                    _logger.LogWarning("Chưa có kho tên cho quốc gia {Nation} — bỏ qua đôn cầu thủ trẻ cho CLB {Club}.", club.Nation, club.Name);
                    continue;
                }

                var surname = surnames[rand.Next(surnames.Count)];
                var givenName = givenNames[rand.Next(givenNames.Count)];
                var fullName = club.Nation == "Vietnam" ? $"{surname} {givenName}" : $"{givenName} {surname}";

                int quality = Math.Max(1, club.YouthTrainingQuality + PickYouthQualityDelta(rand));
                int potential = quality + 15 + rand.Next(0, 21); // +15..+35

                var newPlayer = new Footballer
                {
                    Name = fullName,
                    Age = 16 + rand.Next(0, 3), // 16-18
                    Nation = club.Nation,
                    Position = (PlayerPosition)rand.Next(0, 10),
                    Quality = quality,
                    Potential = potential,
                    ClubId = club.Id,
                    ContractYears = 3,
                    Status = PlayerLifeCycle.Youth,
                    IsTransferListed = false
                };

                await footballerRepo.AddAsync(newPlayer);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private static int PickYouthQualityDelta(Random rand)
        {
            double total = YouthQualityDeltas.Sum(d => d.Weight);
            double roll = rand.NextDouble() * total;
            double cumulative = 0;
            foreach (var (delta, weight) in YouthQualityDeltas)
            {
                cumulative += weight;
                if (roll <= cumulative) return delta;
            }
            return 0;
        }

        private async Task GenerateKnockoutNextRoundAsync(Tournament tournament, int seasonNumber, int round, int week)
        {
            var previousRound = await _matchRepo.GetMatchesByTournamentAndRoundAsync(tournament.Id, round - 1);
            if (!previousRound.Any() || previousRound.Any(m => !m.IsPlayed))
            {
                _logger.LogWarning("Chưa đủ kết quả Round {Prev} của {Name} để sinh Round {Round}.", round - 1, tournament.Name, round);
                return;
            }

            var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id);
            var rankByClub = standings.ToDictionary(tc => tc.ClubId, tc => tc.Rank);
            var winners = previousRound.Select(m => DetermineWinner(m, rankByClub)).ToList();

            var matches = new List<Match>();
            for (int i = 0; i + 1 < winners.Count; i += 2)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    SeasonNumber = seasonNumber,
                    Week = week,
                    Round = round,
                    HomeClubId = winners[i],
                    AwayClubId = winners[i + 1],
                    IsPlayed = false
                });
            }

            if (matches.Any())
                await _matchRepo.AddRangeAsync(matches);
        }

        // Vòng bảng kết thúc (Round 3) -> lấy nhất/nhì mỗi bảng, ghép chéo vào Bán kết
        private async Task GenerateC1SemifinalFromGroupsAsync(Tournament tournament, int seasonNumber, int week, int round)
        {
            var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id); // đã sắp theo Points/GD/GF giảm dần
            var groupA = standings.Where(tc => tc.Group == 1).ToList();
            var groupB = standings.Where(tc => tc.Group == 2).ToList();

            if (groupA.Count < 2 || groupB.Count < 2)
            {
                _logger.LogWarning("Giải {Name} không đủ đội mỗi bảng để sinh Bán kết (Bảng 1: {A}, Bảng 2: {B}).",
                    tournament.Name, groupA.Count, groupB.Count);
                return;
            }

            var matches = new List<Match>
            {
                new Match
                {
                    TournamentId = tournament.Id, SeasonNumber = seasonNumber, Week = week, Round = round,
                    HomeClubId = groupA[0].ClubId, AwayClubId = groupB[1].ClubId, IsPlayed = false
                },
                new Match
                {
                    TournamentId = tournament.Id, SeasonNumber = seasonNumber, Week = week, Round = round,
                    HomeClubId = groupB[0].ClubId, AwayClubId = groupA[1].ClubId, IsPlayed = false
                }
            };

            await _matchRepo.AddRangeAsync(matches);
        }

        private static int DetermineWinner(Match match, Dictionary<int, int> rankByClub)
        {
            if (match.Result == MatchResult.HomeWin) return match.HomeClubId;
            if (match.Result == MatchResult.AwayWin) return match.AwayClubId ?? match.HomeClubId;

            // Hoà: đội có seed (Rank) tốt hơn đi tiếp — không mô phỏng hiệp phụ/luân lưu
            var homeRank = rankByClub.GetValueOrDefault(match.HomeClubId, int.MaxValue);
            var awayRank = match.AwayClubId.HasValue ? rankByClub.GetValueOrDefault(match.AwayClubId.Value, int.MaxValue) : int.MaxValue;
            return homeRank <= awayRank ? match.HomeClubId : match.AwayClubId!.Value;
        }

        private async Task<List<Tournament>> CloneTournamentsFromPreviousSeasonAsync(int seasonNumber)
        {
            var previous = await _tournamentRepo.GetTournamentsBySeasonAsync(seasonNumber - 1);
            var cloned = new List<Tournament>();

            foreach (var prev in previous)
            {
                var next = new Tournament
                {
                    SeasonNumber = seasonNumber,
                    Name = prev.Name,
                    TeamsCount = prev.TeamsCount,
                    Type = prev.Type,
                    PlayersPerMatch = prev.PlayersPerMatch,
                    Nation = prev.Nation,
                    RewardByRank = prev.RewardByRank
                };
                await _tournamentRepo.AddAsync(next);
                cloned.Add(next);
            }

            if (cloned.Any())
                await _unitOfWork.SaveChangesAsync(); // cần Id trước khi tạo TournamentClub/Match tham chiếu tới

            return cloned;
        }

        private async Task AssignNationalClubsAsync(Tournament tournament, int seasonNumber, Dictionary<string, List<int>> seedRanksByNation)
        {
            var nation = tournament.Nation!;
            var existing = await _tournamentRepo.GetStandingsAsync(tournament.Id);
            if (existing.Any())
            {
                seedRanksByNation[nation] = existing.OrderBy(tc => tc.Rank).Select(tc => tc.ClubId).ToList();
                return;
            }

            var clubs = await _clubRepo.GetClubsByNationAsync(nation);
            var seeded = await SeedClubsAsync(clubs, tournament, seasonNumber);

            if (seeded.Count < tournament.TeamsCount)
            {
                _logger.LogWarning("Giải {Name} cần {Need} CLB nhưng chỉ tìm thấy {Have} CLB quốc gia {Nation}.",
                    tournament.Name, tournament.TeamsCount, seeded.Count, nation);
            }

            var selected = seeded.Take(tournament.TeamsCount).ToList();
            var repo = _unitOfWork.Repository<TournamentClub>();
            var clubIds = new List<int>();
            for (int i = 0; i < selected.Count; i++)
            {
                await repo.AddAsync(new TournamentClub
                {
                    TournamentId = tournament.Id,
                    ClubId = selected[i].Id,
                    Rank = i + 1
                });
                clubIds.Add(selected[i].Id);
            }

            seedRanksByNation[nation] = clubIds;
        }

        private async Task<List<Club>> SeedClubsAsync(List<Club> clubs, Tournament tournament, int seasonNumber)
        {
            if (seasonNumber > 1)
            {
                var prevTournaments = await _tournamentRepo.GetTournamentsBySeasonAsync(seasonNumber - 1);
                var prevTournament = prevTournaments.FirstOrDefault(t => t.Name == tournament.Name);
                if (prevTournament != null)
                {
                    var prevStandings = await _tournamentRepo.GetStandingsAsync(prevTournament.Id);
                    var order = prevStandings.Select(tc => tc.ClubId).ToList();
                    return clubs
                        .OrderBy(c => order.IndexOf(c.Id) is var idx && idx >= 0 ? idx : int.MaxValue)
                        .ThenByDescending(c => AverageQuality(c))
                        .ToList();
                }
            }

            // Mùa 1 (hoặc không tìm thấy giải cùng tên mùa trước): seed theo quality trung bình đội hình
            return clubs.OrderByDescending(c => AverageQuality(c)).ToList();
        }

        private static double AverageQuality(Club club) => club.Footballers.Any() ? club.Footballers.Average(f => f.Quality) : 0;

        // Top 2 CLB mỗi giải quốc nội -> gộp đủ TeamsCount, chia luân phiên vào 2 bảng (bảng 1 = các đội #1, bảng 2 = các đội #2)
        private async Task AssignContinentalClubsAsync(Tournament tournament, Dictionary<string, List<int>> seedRanksByNation)
        {
            var existing = await _tournamentRepo.GetStandingsAsync(tournament.Id);
            if (existing.Any()) return; // idempotent

            var qualifiers = seedRanksByNation.Values
                .SelectMany(clubIds => clubIds.Take(2))
                .Take(tournament.TeamsCount)
                .ToList();

            if (qualifiers.Count < tournament.TeamsCount)
            {
                _logger.LogWarning("Giải châu lục {Name} cần {Need} CLB nhưng chỉ gom được {Have} CLB đủ điều kiện.",
                    tournament.Name, tournament.TeamsCount, qualifiers.Count);
            }

            var repo = _unitOfWork.Repository<TournamentClub>();
            for (int i = 0; i < qualifiers.Count; i++)
            {
                await repo.AddAsync(new TournamentClub
                {
                    TournamentId = tournament.Id,
                    ClubId = qualifiers[i],
                    Rank = i + 1,
                    Group = tournament.Type == TournamentType.C1 ? (i % 2) + 1 : null
                });
            }
        }

        private async Task GenerateLeagueFixturesAsync(Tournament tournament, int seasonNumber, List<ScheduleTemplate> templates)
        {
            var existingRound1 = await _matchRepo.GetMatchesByTournamentAndRoundAsync(tournament.Id, 1);
            if (existingRound1.Any()) return; // idempotent

            var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id);
            var clubIds = standings.OrderBy(tc => tc.Rank).Select(tc => tc.ClubId).ToList();

            if (!ValidateEvenClubCount(clubIds, tournament.Name)) return;

            var rounds = GenerateDoubleRoundRobin(clubIds);
            var matches = BuildMatchesFromRounds(tournament, seasonNumber, templates, TournamentType.League, rounds, group: null);

            if (matches.Any())
                await _matchRepo.AddRangeAsync(matches);
        }

        // Vòng bảng C1: chia 2 bảng theo Group đã gán ở AssignContinentalClubsAsync, mỗi bảng đá vòng tròn 1 lượt
        private async Task GenerateGroupStageFixturesAsync(Tournament tournament, int seasonNumber, List<ScheduleTemplate> templates)
        {
            var existingRound1 = await _matchRepo.GetMatchesByTournamentAndRoundAsync(tournament.Id, 1);
            if (existingRound1.Any()) return; // idempotent

            var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id);
            var matches = new List<Match>();

            foreach (var group in new[] { 1, 2 })
            {
                var clubIds = standings.Where(tc => tc.Group == group).Select(tc => tc.ClubId).ToList();
                if (!ValidateEvenClubCount(clubIds, $"{tournament.Name} - Bảng {group}")) continue;

                var rounds = GenerateSingleRoundRobin(clubIds);
                matches.AddRange(BuildMatchesFromRounds(tournament, seasonNumber, templates, TournamentType.C1, rounds, group));
            }

            if (matches.Any())
                await _matchRepo.AddRangeAsync(matches);
        }

        private async Task GenerateKnockoutRoundOneAsync(Tournament tournament, int seasonNumber, List<ScheduleTemplate> templates)
        {
            var existing = await _matchRepo.GetMatchesByTournamentAndRoundAsync(tournament.Id, 1);
            if (existing.Any()) return; // idempotent

            var standings = await _tournamentRepo.GetStandingsAsync(tournament.Id);
            var seeds = standings.OrderBy(tc => tc.Rank).Select(tc => tc.ClubId).ToList();

            if (seeds.Count < 2)
            {
                _logger.LogWarning("Giải {Name} không đủ CLB để sinh Round 1 (cần >=2, có {Count}).", tournament.Name, seeds.Count);
                return;
            }

            var week = templates.FirstOrDefault(t => t.TournamentType == tournament.Type && t.Round == 1)?.Week;
            if (week == null)
            {
                _logger.LogWarning("Không tìm thấy ScheduleTemplate cho {Type} Round 1 — bỏ qua sinh lịch {Name}.", tournament.Type, tournament.Name);
                return;
            }

            var matches = new List<Match>();
            int i = 0, j = seeds.Count - 1;
            while (i < j) // hạng 1 vs hạng cuối, hạng 2 vs hạng áp cuối...
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    SeasonNumber = seasonNumber,
                    Week = week.Value,
                    Round = 1,
                    HomeClubId = seeds[i],
                    AwayClubId = seeds[j],
                    IsPlayed = false
                });
                i++; j--;
            }

            if (matches.Any())
                await _matchRepo.AddRangeAsync(matches);
        }

        private bool ValidateEvenClubCount(List<int> clubIds, string label)
        {
            if (clubIds.Count < 2)
            {
                _logger.LogWarning("{Label} không đủ CLB để sinh lịch (cần >=2, có {Count}).", label, clubIds.Count);
                return false;
            }
            if (clubIds.Count % 2 != 0)
            {
                _logger.LogWarning("{Label} có số CLB lẻ ({Count}) — round-robin hiện chỉ hỗ trợ số chẵn, bỏ qua sinh lịch.", label, clubIds.Count);
                return false;
            }
            return true;
        }

        private List<Match> BuildMatchesFromRounds(
            Tournament tournament, int seasonNumber, List<ScheduleTemplate> templates,
            TournamentType scheduleType, List<List<(int home, int away)>> rounds, int? group)
        {
            var matches = new List<Match>();
            for (int i = 0; i < rounds.Count; i++)
            {
                int round = i + 1;
                var week = templates.FirstOrDefault(t => t.TournamentType == scheduleType && t.Round == round)?.Week;
                if (week == null)
                {
                    _logger.LogWarning("Không tìm thấy ScheduleTemplate cho {Type} Round {Round} — bỏ qua các round còn lại của {Name}.",
                        scheduleType, round, tournament.Name);
                    break;
                }

                foreach (var (home, away) in rounds[i])
                {
                    matches.Add(new Match
                    {
                        TournamentId = tournament.Id,
                        SeasonNumber = seasonNumber,
                        Week = week.Value,
                        Round = round,
                        Group = group,
                        HomeClubId = home,
                        AwayClubId = away,
                        IsPlayed = false
                    });
                }
            }
            return matches;
        }

        // Circle method, 1 lượt: N đội (chẵn) -> (N-1) vòng
        private static List<List<(int home, int away)>> GenerateSingleRoundRobin(List<int> clubIds)
        {
            var n = clubIds.Count;
            var rounds = new List<List<(int, int)>>();
            var ids = new List<int>(clubIds);

            for (int round = 0; round < n - 1; round++)
            {
                var pairs = new List<(int, int)>();
                for (int i = 0; i < n / 2; i++)
                {
                    int home = ids[i];
                    int away = ids[n - 1 - i];
                    // Đảo sân luân phiên theo round để tránh 1 đội đá sân nhà toàn bộ lượt
                    pairs.Add(round % 2 == 0 ? (home, away) : (away, home));
                }
                rounds.Add(pairs);

                // Xoay vòng: giữ ids[0] cố định, các phần tử còn lại xoay 1 vị trí
                var last = ids[n - 1];
                for (int i = n - 1; i > 1; i--)
                    ids[i] = ids[i - 1];
                ids[1] = last;
            }

            return rounds;
        }

        // 2 lượt: lượt đi (circle method) + lượt về (đảo sân toàn bộ)
        private static List<List<(int home, int away)>> GenerateDoubleRoundRobin(List<int> clubIds)
        {
            var firstLeg = GenerateSingleRoundRobin(clubIds);
            var secondLeg = firstLeg.Select(r => r.Select(p => (p.Item2, p.Item1)).ToList()).ToList();

            var allRounds = new List<List<(int, int)>>();
            allRounds.AddRange(firstLeg);
            allRounds.AddRange(secondLeg);
            return allRounds;
        }
    }
}
