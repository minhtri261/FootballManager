using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class TournamentMatchService
    {
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IMatchRepository _matchRepo;

        public TournamentMatchService(
            ITournamentRepository tournamentRepo,
            IMatchRepository matchRepo)
        {
            _tournamentRepo = tournamentRepo;
            _matchRepo = matchRepo;
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

            int n = clubIds.Count;
            int rounds = n - 1;
            int half = n / 2;
            var matches = new List<Match>();

            // ---- Lượt đi ----
            for (int round = 1; round <= rounds; round++)
            {
                for (int i = 0; i < half; i++)
                {
                    int home = clubIds[i];
                    int away = clubIds[n - 1 - i];
                    if (home == -1 || away == -1) continue;

                    matches.Add(new Match
                    {
                        TournamentId = tournamentId,
                        HomeClubId = home,
                        AwayClubId = away
                    });
                }

                // Xoay đội (round robin)
                var temp = clubIds[1];
                clubIds.RemoveAt(1);
                clubIds.Add(temp);
            }

            // ---- Lượt về (đảo ngược sân) ----
            var returnMatches = matches
                .Select(m => new Match
                {
                    TournamentId = tournamentId,
                    HomeClubId = m.AwayClubId,
                    AwayClubId = m.HomeClubId,
                })
                .ToList();

            matches.AddRange(returnMatches);

            await _matchRepo.AddRangeAsync(matches);
        }


        // ------------------- CUP -------------------
        private async Task GenerateCupMatches(int tournamentId, List<int> clubIds)
        {
            var random = new Random();
            clubIds = clubIds.OrderBy(_ => random.Next()).ToList();

            if (clubIds.Count % 2 != 0)
                clubIds.Add(-1);

            var matches = new List<Match>();

            for (int i = 0; i < clubIds.Count; i += 2)
            {
                if (clubIds[i] == -1 || clubIds[i + 1] == -1) continue;

                matches.Add(new Match
                {
                    TournamentId = tournamentId,
                    HomeClubId = clubIds[i],
                    AwayClubId = clubIds[i + 1],
                });
            }

            await _matchRepo.AddRangeAsync(matches);
        }

        // ------------------- C1 -------------------
        private async Task GenerateC1Matches(int tournamentId, List<int> clubIds)
        {
            int groupSize = 4;
            int groupCount = (int)Math.Ceiling((double)clubIds.Count / groupSize);
            var random = new Random();
            clubIds = clubIds.OrderBy(_ => random.Next()).ToList();

            var matches = new List<Match>();

            for (int g = 0; g < groupCount; g++)
            {
                var groupTeams = clubIds.Skip(g * groupSize).Take(groupSize).ToList();
                if (groupTeams.Count < 2) continue;

                for (int i = 0; i < groupTeams.Count; i++)
                {
                    for (int j = i + 1; j < groupTeams.Count; j++)
                    {
                        matches.Add(new Match
                        {
                            TournamentId = tournamentId,
                            HomeClubId = groupTeams[i],
                            AwayClubId = groupTeams[j],
                        });
                    }
                }
            }

            await _matchRepo.AddRangeAsync(matches);
        }

        //Xem danh sách trận đấu theo giải đấu  
        public async Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId)
        {
            return await _matchRepo.GetMatchesByTournamentAsync(tournamentId);
        }
    } 
}
