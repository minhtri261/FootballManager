using FootballManager.Business.DTOs;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.Services
{
    public class RandomResultService
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IMatchLineupRepository _matchLineupRepository;

        public RandomResultService(IMatchRepository matchRepository, IMatchLineupRepository matchLineupRepository)
        {
            _matchRepository = matchRepository;
            _matchLineupRepository = matchLineupRepository;
        }

        // Lấy danh sách các trận chưa đá kèm đội hình để chuẩn bị mô phỏng
        public async Task<List<MatchSimulationDto>> GetUnplayedMatchesForSimulationAsync(int tournamentId)
        {
            // lấy danh sách trận của vòng đang chờ đá (repo đã xử lý round)
            var matches = await _matchRepository.GetUnplayedMatchesByTournamentAsync(tournamentId);

            if (!matches.Any())
                return new List<MatchSimulationDto>();

            var result = new List<MatchSimulationDto>();

            foreach (var match in matches)
            {
                var homeLineup = await _matchLineupRepository
                    .GetByMatchAndClubAsync(match.Id, match.HomeClubId);

                var awayLineup = match.AwayClubId != null
                    ? await _matchLineupRepository.GetByMatchAndClubAsync(match.Id, match.AwayClubId.Value)
                    : null;

                result.Add(new MatchSimulationDto
                {
                    MatchId = match.Id,
                    HomeClubId = match.HomeClubId,
                    AwayClubId = match.AwayClubId ?? 0,

                    HomeLineup = homeLineup,
                    AwayLineup = awayLineup,

                    HomeGoals = 0,
                    AwayGoals = 0
                });
            }

            return result;
        }

        // Hàm random kết quả các trận đấu chưa đá trong giải đấu
        public async Task<List<MatchSimulationDto>> RandomizeResultsAsync(int tournamentId)
        {
            // Lấy tất cả trận chưa đá kèm đội hình
            var matches = await GetUnplayedMatchesForSimulationAsync(tournamentId);
            if (!matches.Any())
                return matches;

            var rnd = new Random();

            foreach (var m in matches)
            {
                // 1) Base Attack/Defense dựa sơ đồ
                double homeAtk = GetFormationAttack(m.HomeLineup?.Formation);
                double homeDef = GetFormationDefense(m.HomeLineup?.Formation);

                double awayAtk = GetFormationAttack(m.AwayLineup?.Formation);
                double awayDef = GetFormationDefense(m.AwayLineup?.Formation);

                // 2) Buff/Nerf theo Status
                homeAtk += GetStatusBuff(m.HomeLineup);
                homeDef += GetStatusBuff(m.HomeLineup, isDefense: true);

                awayAtk += GetStatusBuff(m.AwayLineup);
                awayDef += GetStatusBuff(m.AwayLineup, isDefense: true);

                // 3) Chênh lệch QUALITY theo vị trí
                homeAtk += GetQualityEffect(m.HomeLineup, m.AwayLineup);
                homeDef += GetQualityEffect(m.HomeLineup, m.AwayLineup, isDefense: true);

                awayAtk += GetQualityEffect(m.AwayLineup, m.HomeLineup);
                awayDef += GetQualityEffect(m.AwayLineup, m.HomeLineup, isDefense: true);

                // 4) Phong độ gần nhất
                homeAtk += await GetFormEffect(m.TournamentId, m.HomeClubId);
                awayAtk += await GetFormEffect(m.TournamentId, m.AwayClubId);


                // 5) Lợi thế sân nhà
                homeAtk *= 1.10;
                homeDef *= 1.05;

                // ------------ RANDOM GOALS ----------------

                int homeGoals = PoissonRandom(rnd, Math.Max(0.2, (homeAtk - awayDef) * 0.15));
                int awayGoals = PoissonRandom(rnd, Math.Max(0.2, (awayAtk - homeDef) * 0.15));

                if (homeGoals < 0) homeGoals = 0;
                if (awayGoals < 0) awayGoals = 0;

                m.HomeGoals = homeGoals;
                m.AwayGoals = awayGoals;
            }

            return matches;
        }

        private double GetFormationAttack(string? f)
        {
            return f switch
            {
                "3-2-1" => 1.4,
                "2-3-1" => 1.5,
                "2-2-2" => 1.3,
                "1-3-2" => 1.6,
                _ => 1.2
            };
        }

        private double GetFormationDefense(string? f)
        {
            return f switch
            {
                "3-2-1" => 1.6,
                "2-3-1" => 1.4,
                "2-2-2" => 1.3,
                "1-3-2" => 1.2,
                _ => 1.1
            };
        }

        private double GetStatusBuff(MatchLineup? lu, bool isDefense = false)
        {
            if (lu == null) return 0;

            double sum = 0;

            foreach (var p in lu.Players)
            {
                var s = p.Footballer.Status;

                if (!isDefense)
                {
                    sum += s switch
                    {
                        "Trẻ" => -0.05,
                        "Trỗi dậy" => 0.15,
                        "Đỉnh cao" => 0.20,
                        "Ổn định" => 0.05,
                        "Lão tướng" => -0.05,
                        _ => 0
                    };
                }
                else
                {
                    sum += s switch
                    {
                        "Trẻ" => -0.10,
                        "Trỗi dậy" => 0.05,
                        "Đỉnh cao" => 0.20,
                        "Ổn định" => 0.20,
                        "Lão tướng" => 0.10,
                        _ => 0
                    };
                }
            }

            return sum / lu.Players.Count;
        }

        private double GetQualityEffect(MatchLineup? atk, MatchLineup? def, bool isDefense = false)
        {
            if (atk == null || def == null) return 0;

            double atkScore = atk.Players.Sum(p => p.Footballer.Quality);
            double defScore = def.Players.Sum(p => p.Footballer.Quality);

            double diff = (atkScore - defScore) / Math.Max(1, atk.Players.Count);

            if (!isDefense)
                return diff * 0.03; // công
            else
                return -diff * 0.02; // thủ
        }

        private int PoissonRandom(Random rnd, double lambda)
        {
            double L = Math.Exp(-lambda);
            double p = 1.0;
            int k = 0;

            do
            {
                k++;
                p *= rnd.NextDouble();
            }
            while (p > L);

            return k - 1;
        }


        private async Task<double> GetFormEffect(int tournamentId, int clubId)
        {
            //sửa lại cho tôi
            var lastMatches = await _matchRepository.GetLastFiveMatchesAsync(tournamentId, clubId);

            if (lastMatches == null || lastMatches.Count == 0)
                return 0;

            int wins = 0;

            foreach (var m in lastMatches)
            {
                bool isHome = (m.HomeClubId == clubId);

                int goalsFor = isHome ? m.HomeGoals : m.AwayGoals;
                int goalsAgainst = isHome ? m.AwayGoals : m.HomeGoals;

                if (goalsFor > goalsAgainst)
                    wins++;
            }

            return wins switch
            {
                3 => 0.25,
                2 => 0.15,
                1 => 0.05,
                0 => -0.10,
                _ => 0
            };
        }
            
    }
}
