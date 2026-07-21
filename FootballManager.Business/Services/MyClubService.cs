using FootballManager.Business.DTOs;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FootballManager.Business.Services
{
    public class MyClubService : IMyClubService
    {
        private readonly IClubRepository _clubRepo;
        private readonly IMatchRepository _matchRepo;
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GameSettings _settings;

        public MyClubService(
            IClubRepository clubRepo,
            IMatchRepository matchRepo,
            ITournamentRepository tournamentRepo,
            IUnitOfWork unitOfWork,
            IOptions<GameSettings> settings)
        {
            _clubRepo = clubRepo;
            _matchRepo = matchRepo;
            _tournamentRepo = tournamentRepo;
            _unitOfWork = unitOfWork;
            _settings = settings.Value;
        }

        public async Task<Club?> GetMyClubAsync() => await _clubRepo.GetByIdAsync(_settings.MyClubId);

        public async Task<IEnumerable<FootballerDto>> GetMyPlayersAsync()
        {
            var club = await _clubRepo.GetByIdWithPlayersAsync(_settings.MyClubId);
            if (club == null) return Enumerable.Empty<FootballerDto>();

            return club.Footballers.Select(f => new FootballerDto
            {
                Id = f.Id,
                Name = f.Name,
                Age = f.Age,
                Nation = f.Nation,
                Position = f.Position.ToString(),
                Quality = f.Quality,
                ClubId = f.ClubId,
                ClubName = club.Name,
                ContractYears = f.ContractYears,
                Status = f.Status,
                AwardQBV = f.AwardQBV,
                AwardQBB = f.AwardQBB,
                AwardQBD = f.AwardQBD,
                IsTransferListed = f.IsTransferListed
            });
        }

        public async Task<NextMatchDto?> GetNextMatchAsync()
        {
            var match = await _matchRepo.GetNextMatchForClubAsync(_settings.MyClubId);
            if (match == null) return null;

            bool isHome = match.HomeClubId == _settings.MyClubId;
            var opponent = isHome ? match.AwayClub : match.HomeClub;

            return new NextMatchDto
            {
                MatchId = match.Id,
                TournamentId = match.TournamentId,
                TournamentName = match.Tournament?.Name ?? string.Empty,
                Week = match.Week,
                Round = match.Round,
                IsHome = isHome,
                OpponentClubId = isHome ? match.AwayClubId : match.HomeClubId,
                OpponentClubName = opponent?.Name,
                HasSubmittedLineup = match.MatchLineups.Any(l => l.ClubId == _settings.MyClubId),
                PlayersPerMatch = match.Tournament?.PlayersPerMatch ?? 7
            };
        }

        public async Task<OpponentLineupDto?> GetNextOpponentLineupAsync()
        {
            var match = await _matchRepo.GetNextMatchForClubAsync(_settings.MyClubId);
            if (match == null) return null;

            bool isHome = match.HomeClubId == _settings.MyClubId;
            var opponentClubId = isHome ? match.AwayClubId : match.HomeClubId;
            var opponentClub = isHome ? match.AwayClub : match.HomeClub;
            if (opponentClubId == null || opponentClub == null) return null;

            var lineup = match.MatchLineups.FirstOrDefault(l => l.ClubId == opponentClubId);
            if (lineup == null) return null; // Đối thủ chưa có đội hình cho trận này

            return new OpponentLineupDto
            {
                MatchId = match.Id,
                OpponentClubId = opponentClubId.Value,
                OpponentClubName = opponentClub.Name,
                Formation = lineup.Formation,
                Players = lineup.Players
                    .Where(p => p.Footballer != null)
                    .Select(p => new OpponentPlayerDto
                    {
                        Name = p.Footballer!.Name,
                        Age = p.Footballer!.Age,
                        Nation = p.Footballer!.Nation,
                        Position = p.Footballer!.Position.ToString()
                    }).ToList()
            };
        }

        public async Task SubmitLineupAsync(SubmitLineupDto dto)
        {
            var myClubId = _settings.MyClubId;

            var match = await _matchRepo.GetByIdAsync(dto.MatchId);
            if (match == null) throw new Exception("Không tìm thấy trận đấu.");
            if (match.HomeClubId != myClubId && match.AwayClubId != myClubId)
                throw new Exception("Trận đấu này không thuộc CLB của bạn.");
            if (match.IsPlayed)
                throw new Exception("Trận đấu đã diễn ra, không thể nộp đội hình.");

            var tournament = await _tournamentRepo.GetByIdAsync(match.TournamentId);
            int required = tournament?.PlayersPerMatch ?? 7;
            if (dto.PlayerIds.Count != required)
                throw new Exception($"Cần chọn đúng {required} cầu thủ.");

            var myPlayerIds = (await GetMyPlayersAsync()).Select(p => p.Id).ToHashSet();
            if (dto.PlayerIds.Any(id => !myPlayerIds.Contains(id)))
                throw new Exception("Có cầu thủ không thuộc CLB của bạn.");

            var lineupRepo = _unitOfWork.Repository<MatchLineup>();
            var existingLineup = await lineupRepo.GetAll()
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.MatchId == dto.MatchId && l.ClubId == myClubId);

            if (existingLineup != null)
            {
                var playerRepo = _unitOfWork.Repository<MatchLineupPlayer>();
                foreach (var p in existingLineup.Players.ToList())
                    playerRepo.Delete(p);

                existingLineup.Formation = dto.Formation;
                lineupRepo.Update(existingLineup);

                foreach (var playerId in dto.PlayerIds)
                {
                    await playerRepo.AddAsync(new MatchLineupPlayer { MatchLineupId = existingLineup.Id, FootballerId = playerId });
                }
            }
            else
            {
                var lineup = new MatchLineup
                {
                    MatchId = dto.MatchId,
                    ClubId = myClubId,
                    Formation = dto.Formation,
                    Players = dto.PlayerIds.Select(id => new MatchLineupPlayer { FootballerId = id }).ToList()
                };
                await lineupRepo.AddAsync(lineup);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<MatchResultSummaryDto?> GetLastMatchResultAsync()
        {
            var match = await _matchRepo.GetLastPlayedMatchForClubAsync(_settings.MyClubId);
            if (match == null) return null;

            bool isHome = match.HomeClubId == _settings.MyClubId;
            var opponent = isHome ? match.AwayClub : match.HomeClub;

            return new MatchResultSummaryDto
            {
                MatchId = match.Id,
                TournamentName = match.Tournament?.Name ?? string.Empty,
                Week = match.Week,
                Round = match.Round,
                IsHome = isHome,
                OpponentClubName = opponent?.Name,
                HomeGoals = match.HomeGoals,
                AwayGoals = match.AwayGoals,
                Result = match.Result
            };
        }
    }
}
