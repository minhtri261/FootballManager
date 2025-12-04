using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.Extensions.Options;


namespace FootballManager.Business.Services
{
    public class MatchService : BaseService<Match>, IMatchService
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IBotLineupService _botLineupService;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IMatchLineupRepository _matchLineupRepository;
        private readonly IClubRepository _clubRepository;
        private readonly RandomResultService randomResultService;
        private readonly GameSettings _settings;

        public MatchService(IMatchRepository repo, IOptions<GameSettings> settings, IBotLineupService botLineupService, ITournamentRepository tournamentRepository, IMatchLineupRepository matchLineupRepository, IClubRepository clubRepository, RandomResultService randomResultService) : base(repo)
        {
            _matchRepository = repo;
            _settings = settings.Value;
            _botLineupService = botLineupService;
            _tournamentRepository = tournamentRepository;
            _matchLineupRepository = matchLineupRepository;
            _clubRepository = clubRepository;
            this.randomResultService = randomResultService;
        }
        public async Task PrepareRoundForAllTournamentAsync(int seasonNumber)
        {
            var tournaments = await _tournamentRepository.GetBySeasonNumberAsync(seasonNumber);

            var league = tournaments.FirstOrDefault(t => t.Type == TournamentType.League);
            var cup = tournaments.FirstOrDefault(t => t.Type == TournamentType.Cup);

            if (league == null) return;

            var leagueUnplayed = await _matchRepository.GetUnplayedMatchesByTournamentAsync(league.Id);
            var cupUnplayed = cup != null
                ? await _matchRepository.GetUnplayedMatchesByTournamentAsync(cup.Id)
                : new List<Match>();

            // 1) Nếu cả league & cup đều hết trận → GỌI HÀM TỔNG KẾT SEASON
            if (!leagueUnplayed.Any() && !cupUnplayed.Any())
            {
                // TODO: tổng kết mùa giải:
                // await _seasonService.CompleteSeasonAsync(seasonNumber);
                return;
            }

            // 2) Chọn giải sẽ đá tiếp theo
            int leagueRound = leagueUnplayed.Any() ? leagueUnplayed.Min(m => m.Round) : int.MaxValue;
            int cupRound = cupUnplayed.Any() ? cupUnplayed.Min(m => m.Round) : int.MaxValue;

            // ✅ Logic xen kẽ:
            // Cup chỉ được đá khi League >= Cup * 2
            int targetTournamentId;

            if (cupUnplayed.Any() && leagueRound >= cupRound * 2)
                targetTournamentId = cup.Id;
            else
                targetTournamentId = league.Id;

            var currentMatches = (targetTournamentId == league.Id ? leagueUnplayed : cupUnplayed)
                .Where(m => m.Round == (targetTournamentId == league.Id ? leagueRound : cupRound))
                .ToList();

            //3) Tạo bot lineup hoặc chờ lineup của player
            foreach (var match in currentMatches)
            {
                if (match.HomeClubId != _settings.MyClubId &&
                    match.AwayClubId != _settings.MyClubId)
                {
                    if (match.HomeClubId != 0 && match.AwayClubId != null)
                    {
                        await _botLineupService.CreateBotLineupAsync(match.TournamentId, match.HomeClubId, match.AwayClubId.Value, match.Id);
                        await _botLineupService.CreateBotLineupAsync(match.TournamentId, match.AwayClubId.Value, match.HomeClubId, match.Id);
                    }

                    continue;
                }

                // Nếu đội người chơi tham gia → tạo lineup cho đội còn lại
                int botClubId =
                    match.HomeClubId == _settings.MyClubId
                    ? match.AwayClubId ?? 0
                    : match.HomeClubId;

                if (botClubId != 0 && botClubId != _settings.MyClubId)
                    await _botLineupService.CreateBotLineupAsync(match.TournamentId, botClubId, _settings.MyClubId, match.Id);
            }
        }

        public async Task SaveMatchLineupAsync(SubmitLineupDto dto)
        {
            // 1) Check match có tồn tại
            var match = await _matchRepository.GetByIdAsync(dto.MatchId)
            ?? throw new InvalidOperationException("Match not found");

            // 2. Validate club participates in match
            if (match.HomeClubId != _settings.MyClubId && match.AwayClubId != _settings.MyClubId)
                throw new InvalidOperationException("Club is not part of this match");

            // 3. Determine required number of players from Tournament.PlayersPerMatch
            var tournament = await _tournamentRepository.GetByIdAsync(match.TournamentId)
                ?? throw new InvalidOperationException("Tournament not found");

            int playersPerMatch = Math.Max(1, tournament.PlayersPerMatch); // safe default
                                                                           // For your 7-a-side: playersPerMatch == 7 -> FE likely sends 7 playerIds
            if (dto.PlayerIds == null || dto.PlayerIds.Count != playersPerMatch)
                throw new InvalidOperationException($"Lineup must contain exactly {playersPerMatch} players");

            var oldLineup = await _matchLineupRepository
        .GetByMatchAndClubAsync(dto.MatchId, _settings.MyClubId);

            if (oldLineup != null)
            {
                _matchLineupRepository.Delete(oldLineup);
                await _matchLineupRepository.SaveChangesAsync();
            }

            var newLineup = new MatchLineup
            {
                MatchId = dto.MatchId,
                ClubId = _settings.MyClubId,
                Formation = dto.Formation,
                Players = dto.PlayerIds.Select(id => new MatchLineupPlayer { FootballerId = id }).ToList()
            };

            // 7. Save
            await _matchLineupRepository.AddAsync(newLineup);
            await _matchLineupRepository.SaveChangesAsync();
        }

        public async Task<OpponentLineupDto> GetOpponentLineupAsync(int matchId)
        {
            var match = await _matchRepository.GetByIdAsync(matchId)
                ?? throw new InvalidOperationException("Match not found");

            // xác định club đối thủ
            int opponentClubId =
                match.HomeClubId == _settings.MyClubId
                ? match.AwayClubId ?? 0
                : match.HomeClubId;

            if (opponentClubId == 0)
                throw new InvalidOperationException("Opponent not available");

            var lineup = await _matchLineupRepository
                .GetByMatchAndClubAsync(matchId, opponentClubId)
                ?? throw new InvalidOperationException("Opponent has no lineup yet");

            return new OpponentLineupDto
            {
                ClubName = lineup.Club.Name,
                ClubNation = lineup.Club.Nation,
                Formation = lineup.Formation,
                Players = lineup.Players
                    .Select(p => new OpponentPlayerDto
                    {
                        Name = p.Footballer.Name,
                        Age = p.Footballer.Age,
                        Nation = p.Footballer.Nation,
                        Position = p.Footballer.Position
                    })
                    .ToList()
            };
        }

        //Hàm nhận kết quả random từ RandomResultService và áp dụng vào Match, cập nhật bảng xếp hạng, tiền, v.v.
        public async Task ApplySimulationResultAsync(MatchSimulationDto dto)
        {
            var match = await _matchRepository.GetByIdAsync(dto.MatchId);
            if (match == null) return;

            // ---- CẬP NHẬT TỈ SỐ ----
            match.HomeGoals = dto.HomeGoals;
            match.AwayGoals = dto.AwayGoals;
            match.IsPlayed = true;

            match.Result = dto.HomeGoals > dto.AwayGoals
                ? MatchResult.HomeWin
                : dto.HomeGoals < dto.AwayGoals
                    ? MatchResult.AwayWin
                    : MatchResult.Draw;

            // Lấy lineup thật để random cầu thủ
            var homeLu = dto.HomeLineup;
            var awayLu = dto.AwayLineup;

            var rnd = new Random();

            // ============================================================
            // 1) RANDOM NGƯỜI GHI BÀN
            // ============================================================

            match.Goals.Clear();

            void AddGoal(int count, MatchLineup lineup, int clubId)
            {
                if (lineup == null) return;
                var list = lineup.Players.ToList();
                if (list.Count == 0) return;

                for (int i = 0; i < count; i++)
                {
                    var p = list[rnd.Next(list.Count)];

                    match.Goals.Add(new MatchGoal
                    {
                        MatchId = match.Id,
                        FootballerId = p.FootballerId,
                        ClubId = clubId,
                        IsOwnGoal = false
                    });
                }
            }

            AddGoal(dto.HomeGoals, homeLu, dto.HomeClubId);
            AddGoal(dto.AwayGoals, awayLu, dto.AwayClubId);

            // ============================================================
            // 2) RANDOM MVP (ưu tiên đội thắng, sau đó đội ghi nhiều bàn hơn)
            // ============================================================

            List<MatchLineupPlayer> choosePool;

            if (match.Result == MatchResult.HomeWin)
                choosePool = homeLu?.Players.ToList();
            else if (match.Result == MatchResult.AwayWin)
                choosePool = awayLu?.Players.ToList();
            else
                choosePool = homeLu!.Players.Concat(awayLu!.Players).ToList();

            if (choosePool != null && choosePool.Count > 0)
            {
                var chosen = choosePool[rnd.Next(choosePool.Count)];
                match.MVPFootballerId = chosen.FootballerId;
            }

            // ============================================================
            // 3) CẬP NHẬT BẢNG XẾP HẠNG / TIỀN
            // ============================================================

            var homeTC = await _tournamentRepository.GetTournamentClubAsync(match.TournamentId, match.HomeClubId);
            var awayTC = match.AwayClubId != null
                ? await _tournamentRepository.GetTournamentClubAsync(match.TournamentId, match.AwayClubId.Value)
                : null;

            homeTC.Played += 1;
            homeTC.GoalsFor += dto.HomeGoals;
            homeTC.GoalsAgainst += dto.AwayGoals;

            if (awayTC != null)
            {
                awayTC.Played += 1;
                awayTC.GoalsFor += dto.AwayGoals;
                awayTC.GoalsAgainst += dto.HomeGoals;
            }

            var homeClub = await _clubRepository.GetByIdAsync(match.HomeClubId);
            var awayClub = match.AwayClubId != null
                ? await _clubRepository.GetByIdAsync(match.AwayClubId.Value)
                : null;

            switch (match.Result)
            {
                case MatchResult.HomeWin:
                    homeTC.Won += 1;
                    homeTC.Points += 3;
                    homeClub.Money += 3;

                    if (awayTC != null)
                        awayTC.Lost += 1;
                    break;

                case MatchResult.AwayWin:
                    awayTC!.Won += 1;
                    awayTC.Points += 3;
                    awayClub!.Money += 3;

                    homeTC.Lost += 1;
                    break;

                case MatchResult.Draw:
                    homeTC.Drawn += 1;
                    homeTC.Points += 1;
                    homeClub.Money += 1;

                    if (awayTC != null)
                    {
                        awayTC.Drawn += 1;
                        awayTC.Points += 1;
                        awayClub!.Money += 1;
                    }
                    break;
            }

            // ============================================================
            // 4) RANDOM +1 QUALITY CHO 1 CẦU THỦ ĐỘI THẮNG
            // ============================================================

            if (match.Result == MatchResult.HomeWin && homeLu != null)
            {
                var list = homeLu.Players.ToList();
                var p = list[rnd.Next(list.Count)].Footballer;
                p.Quality += 1;
            }
            else if (match.Result == MatchResult.AwayWin && awayLu != null)
            {
                var list = awayLu.Players.ToList();
                var p = list[rnd.Next(list.Count)].Footballer;
                p.Quality += 1;
            }

            // ============================================================
            // 5) LƯU DB
            // ============================================================

            await _matchRepository.SaveChangesAsync();
        }

        // Lấy danh sách các trận của vòng hiện tại trong giải đấu
        public async Task<List<MatchDto>> GetCurrentRoundMatchesAsync(int tournamentId)
        {
            var matches = await _matchRepository.GetUnplayedMatchesByTournamentAsync(tournamentId);

            return matches.Select(m => new MatchDto
            {
                MatchId = m.Id,
                HomeClubId = m.HomeClubId,
                AwayClubId = m.AwayClubId ?? 0,
                IsPlayed = m.IsPlayed,
                HomeGoals = m.HomeGoals,
                AwayGoals = m.AwayGoals
            }).ToList();
        }

        // Lấy kết quả các trận của vòng vừa kết thúc trong giải đấu
        public async Task<List<MatchResultDto>> GetLastRoundResultsAsync(int tournamentId)
        {
            var allMatches = await _matchRepository.GetLastRoundResultsAsync(tournamentId);
            return allMatches.Select(m => new MatchResultDto
            {
                MatchId = m.Id,
                HomeClubId = m.HomeClubId,
                AwayClubId = m.AwayClubId ?? 0,
                HomeGoals = m.HomeGoals,
                AwayGoals = m.AwayGoals,
                Result = m.Result,
                MVPFootballerId = m.MVPFootballerId,
                Goals = m.Goals.Select(g => new GoalDto
                {
                    ClubId = g.ClubId,
                    FootballerId = g.FootballerId,
                    IsOwnGoal = g.IsOwnGoal
                }).ToList()
            }).ToList();
        }

        // Lấy kết quả trận của Round vừa đá của đội người chơi trong giải đấu
            public async Task<List<MatchResultDto>> MyClubResultLastRound(int tournamentId)
            {
                var matches = await _matchRepository.GetLastRoundResultsForClubAsync(tournamentId, _settings.MyClubId);
                return matches.Select(m => new MatchResultDto
                {
                    MatchId = m.Id,
                    HomeClubId = m.HomeClubId,
                    AwayClubId = m.AwayClubId ?? 0,
                    HomeGoals = m.HomeGoals,
                    AwayGoals = m.AwayGoals,
                    Result = m.Result,
                    MVPFootballerId = m.MVPFootballerId,
                    Goals = m.Goals.Select(g => new GoalDto
                    {
                        ClubId = g.ClubId,
                        FootballerId = g.FootballerId,
                        IsOwnGoal = g.IsOwnGoal
                }).ToList()
                }).ToList();
        }
    }
}
