using FootballManager.Data.Entities;

namespace FootballManager.Business.DTOs
{
    public class TournamentDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TournamentType Type { get; set; }
        public int SeasonNumber { get; set; }

        // BXH: có dữ liệu khi giải đấu theo thể thức vòng tròn (League) hoặc vòng bảng (C1) — rỗng với Cup (knockout thuần)
        public List<StandingDto> Standings { get; set; } = new();

        // Lịch thi đấu theo từng vòng — luôn có, dùng làm bracket cho Cup/C1 knockout hoặc lịch cho League
        public List<RoundDto> Rounds { get; set; } = new();
    }

    public class StandingDto
    {
        public int Rank { get; set; }
        public int? Group { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
    }

    public class RoundDto
    {
        public int Round { get; set; }
        public List<TournamentMatchDto> Matches { get; set; } = new();
    }

    public class TournamentMatchDto
    {
        public int MatchId { get; set; }
        public int? Group { get; set; }
        public int HomeClubId { get; set; }
        public string HomeClubName { get; set; } = string.Empty;
        public int? AwayClubId { get; set; }
        public string? AwayClubName { get; set; }
        public bool IsPlayed { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public MatchResult? Result { get; set; }
    }
}
