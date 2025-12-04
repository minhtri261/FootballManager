using FootballManager.Data.Entities;
using FootballManager.Data.Repositories;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FootballManager.Business.Services
{
    public class TournamentMatchService
    {
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IMatchRepository _matchRepo;
        private readonly ILogger<TournamentMatchService> _logger;
        public TournamentMatchService(
            ITournamentRepository tournamentRepo,
            IMatchRepository matchRepo,
            ILogger<TournamentMatchService> logger
            )
        {
            _tournamentRepo = tournamentRepo;
            _matchRepo = matchRepo;
            _logger = logger;
        }

        public async Task GenerateMatchesAsync(int tournamentId)
        {
            var tournament = await _tournamentRepo.GetByIdAsync(tournamentId)
                              ?? throw new Exception("Giải đấu không tồn tại.");

            var clubIds = tournament.TournamentClubs
                .Select(tc => tc.ClubId)
                .ToList();

            if (clubIds.Count < 2)
                throw new Exception("Cần ít nhất 2 CLB để tạo lịch.");

            switch (tournament.Type)
            {
                case TournamentType.League:
                    await GenerateLeagueMatches(tournamentId, clubIds);
                    break;
                case TournamentType.Cup:
                    await GenerateCupMatches(tournamentId, clubIds);
                    break;
                case TournamentType.C1:
                    await GenerateC1Matches(tournamentId, clubIds);
                    break;
                default:
                    throw new Exception("Loại giải đấu không hợp lệ.");
            }
        }

        // ------------------- LEAGUE -------------------
        private async Task GenerateLeagueMatches(int tournamentId, List<int> clubIds)
        {
            bool hasBye = clubIds.Count % 2 != 0;
            if (hasBye) clubIds.Add(-1);
            _logger.LogInformation("Club count (with bye if any): {Count}", clubIds.Count);
            int n = clubIds.Count;
            int rounds = n - 1;
            int half = n / 2;
            var matches = new List<Match>();

            int roundNumber = 1;
            // ---- Lượt đi ----
            for (int round = 1; round <= rounds; round++)
            {
                _logger.LogInformation("Generating round {Round}", roundNumber);
                for (int i = 0; i < half; i++)
                {
                    int home = clubIds[i];
                    int away = clubIds[n - 1 - i];
                    _logger.LogInformation("Match: {Home} vs {Away}", home, away);
                    if (home == -1 || away == -1)
                    {
                        _logger.LogInformation("One team has a bye this round.");
                        continue;
                    }
                    

                    matches.Add(new Match
                    {
                        TournamentId = tournamentId,
                        HomeClubId = home,
                        AwayClubId = away,
                        Round = roundNumber,
                        Leg = 1,
                        IsPlayed = false
                    });
                }

                // Xoay đội (round robin)
                var temp = clubIds[1];
                clubIds.RemoveAt(1);
                clubIds.Add(temp);

                roundNumber++;
            }

            _logger.LogInformation("Total matches generated for first leg: {Count}", matches.Count);

            int matchCount = matches.Count;
            // ---- Lượt về (đảo ngược sân) ----
            for (int i = 0; i < matchCount; i++)
            {
                _logger.LogInformation("Generating return leg for match: {Home} vs {Away}", matches[i].HomeClubId, matches[i].AwayClubId);
                matches.Add(new Match
                {
                    TournamentId = tournamentId,
                    HomeClubId = matches[i].AwayClubId!.Value,
                    AwayClubId = matches[i].HomeClubId,
                    Round = matches[i].Round + rounds,
                    Leg = 2,
                    IsPlayed = false
                });
            }

            _logger.LogInformation("Total matches generated after second leg: {Count}", matches.Count);
            await _matchRepo.AddRangeAsync(matches);
        }

        // ------------------- CUP -------------------
        private async Task GenerateCupMatches(int tournamentId, List<int> clubIds)
        {
            var random = new Random();
            clubIds = clubIds.OrderBy(_ => random.Next()).ToList();

            var matches = new List<Match>();
            int total = clubIds.Count;

            // Tìm số đội chuẩn dạng 2^n
            int target = 1;
            while (target * 2 <= total)
                target *= 2;

            int teamsPlay = total - target; // số đội cần đá vòng 1
            int teamsInMatches = teamsPlay * 2; // số đội tham gia thi đấu vòng 1

            var playingTeams = clubIds.Take(teamsInMatches).ToList();
            var byeTeams = clubIds.Skip(teamsInMatches).ToList();

            int round = 1;

            // ✅ Tạo các trận vòng 1
            for (int i = 0; i < playingTeams.Count; i += 2)
            {
                matches.Add(new Match
                {
                    TournamentId = tournamentId,
                    HomeClubId = playingTeams[i],
                    AwayClubId = playingTeams[i + 1],
                    Round = round,
                    Leg = 1,
                    IsPlayed = false
                });
            }

            // ✅ Tạo bye đúng chuẩn (không chơi, vào vòng sau)
            foreach (var bye in byeTeams)
            {
                matches.Add(new Match
                {
                    TournamentId = tournamentId,
                    HomeClubId = bye,
                    AwayClubId = null,
                    Round = round,
                    Leg = 0,
                    IsPlayed = false,   // ❗ để sau PrepareRound xử lý lineup
                    Result = MatchResult.HomeWin
                });
            }

            await _matchRepo.AddRangeAsync(matches);
        }



        // ------------------- C1 -------------------
        private async Task GenerateC1Matches(int tournamentId, List<int> clubIds)
        {
            var random = new Random();
            clubIds = clubIds.OrderBy(_ => random.Next()).ToList();

            int groupSize = 4;
            int groupCount = (int)Math.Ceiling((double)clubIds.Count / groupSize);

            var groups = new List<List<int>>();
            int index = 0;

            for (int g = 0; g < groupCount; g++)
                groups.Add(new List<int>());

            foreach (var id in clubIds)
            {
                groups[index % groupCount].Add(id);
                index++;
            }

            // FIX: gom nhóm 2 đội
            foreach (var g in groups.Where(g => g.Count == 2).ToList())
            {
                var target = groups.First(gr => gr.Count < 4 && gr != g);
                target.Add(g[1]);
                g.RemoveAt(1);
            }

            var matches = new List<Match>();
            int groupNumber = 1;

            foreach (var group in groups)
            {
                if (group.Count < 3) continue;

                int round = 1;   // ✅ reset round theo từng bảng

                for (int i = 0; i < group.Count; i++)
                {
                    for (int j = i + 1; j < group.Count; j++)
                    {
                        matches.Add(new Match
                        {
                            TournamentId = tournamentId,
                            HomeClubId = group[i],
                            AwayClubId = group[j],
                            Group = groupNumber,
                            Round = round,
                            Leg = 1,
                            IsPlayed = false
                        });

                        matches.Add(new Match
                        {
                            TournamentId = tournamentId,
                            HomeClubId = group[j],
                            AwayClubId = group[i],
                            Group = groupNumber,
                            Round = round + group.Count - 1,
                            Leg = 2,
                            IsPlayed = false
                        });

                        round++;  // ✅ mỗi cặp tăng 1 round trong bảng
                    }
                }

                groupNumber++;
            }

            await _matchRepo.AddRangeAsync(matches);
        }


        //Xem danh sách trận đấu theo giải đấu  
        public async Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId)
        {
            return await _matchRepo.GetMatchesByTournamentAsync(tournamentId);
        }

        //Hàm tạo vòng tiếp theo cho giải Cup
        public async Task GenerateNextCupRoundAsync(int tournamentId)
        {
            // Lấy toàn bộ trận của giải
            var matches = await _matchRepo.GetMatchesByTournamentAsync(tournamentId);
            if (!matches.Any())
                return;

            // Lấy round lớn nhất đã tạo
            int currentRound = matches.Max(m => m.Round);

            // Lấy tất cả trận của round hiện tại
            var currentRoundMatches = matches
                .Where(m => m.Round == currentRound)
                .ToList();

            // Kiểm tra đã đá đủ chưa
            // Điều kiện hợp lệ để tạo vòng mới:
            //  - trận có Away == null (bye) => OK
            //  - IsPlayed == true => OK
            if (currentRoundMatches.Any(m => m.AwayClubId != null && !m.IsPlayed))
                return; // chưa đủ dữ liệu để tạo vòng tiếp theo

            // ✅ Lấy danh sách đội đi tiếp
            var advancingTeams = new List<int>();

            foreach (var match in currentRoundMatches)
            {
                // bye
                if (match.AwayClubId == null)
                {
                    advancingTeams.Add(match.HomeClubId);
                    continue;
                }

                // đội thắng
                advancingTeams.Add(
                    match.Result == MatchResult.HomeWin
                    ? match.HomeClubId
                    : match.AwayClubId.Value
                );

                // Nếu còn 1 đội -> đã vô địch
                if (advancingTeams.Count <= 1)
                    return;

                // ✅ Random lại để tạo cặp đẹp hơn
                var random = new Random();
                advancingTeams = advancingTeams.OrderBy(_ => random.Next()).ToList();

                var nextRoundMatches = new List<Match>();
                int nextRound = currentRound + 1;

                // ✅ Nếu số đội lẻ -> bye 1 đội
                if (advancingTeams.Count % 2 != 0)
                {
                    int byeTeam = advancingTeams[^1];
                    advancingTeams.RemoveAt(advancingTeams.Count - 1);

                    nextRoundMatches.Add(new Match
                    {
                        TournamentId = tournamentId,
                        HomeClubId = byeTeam,
                        AwayClubId = null,
                        Round = nextRound,
                        Leg = 0,
                        IsPlayed = false,
                        Result = MatchResult.HomeWin
                    });
                }

                // ✅ Tạo các cặp đấu vòng tiếp theo
                for (int i = 0; i < advancingTeams.Count; i += 2)
                {
                    nextRoundMatches.Add(new Match
                    {
                        TournamentId = tournamentId,
                        HomeClubId = advancingTeams[i],
                        AwayClubId = advancingTeams[i + 1],
                        Round = nextRound,
                        Leg = 1,
                        IsPlayed = false
                    });
                }

                await _matchRepo.AddRangeAsync(nextRoundMatches);
            }
        }
    } 
}
