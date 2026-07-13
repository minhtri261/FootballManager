namespace FootballManagerMVC.Models
{
    public class MatchSummaryViewModel
    {
        public int TournamentId { get; set; }

        // Tất cả kết quả vòng trước
        public List<MatchResultDto> LastRoundResults { get; set; } = new();

        // Kết quả trận của tôi
        public MyMatchResultDto? MyMatch { get; set; }

        // BXH
        public List<StandingDto> Standings { get; set; } = new();
    }

    public class MyMatchResultDto
    {
        public bool HasMatch { get; set; }
        public MatchResultDto? Match { get; set; }
    }

    public class MatchResultDto
    {
        public int MatchId { get; set; }
        public int HomeClubId { get; set; }
        public int AwayClubId { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public MatchResult Result { get; set; }
    }

    public class StandingDto
    {
        public int Rank { get; set; }
        public string ClubName { get; set; } = "";
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int Points { get; set; } // Nếu API trả về 0, hãy kiểm tra tc.Points ở DB có giá trị chưa
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public string Status { get; set; } = "Active"; // Thêm để dùng cho giải Cup
    }

}
