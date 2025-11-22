namespace FootballManagerMVC.Models
{
    public class FootballerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Position { get; set; } = "";
        public int Quality { get; set; }
        public int ContractYears { get; set; }
        public string? ClubName { get; set; }
        public bool IsTransferListed { get; set; }
    }

    public class TransferOfferModel
    {
        public int Id { get; set; } // ← thêm Id để hiển thị rõ lời đề nghị nào
        public int FootballerId { get; set; }

        public int? FromClubId { get; set; }
        public string? FromClubName { get; set; }  // ✅ hiển thị tên CLB gửi
        public int ToClubId { get; set; }
        public string? ToClubName { get; set; }    // ✅ hiển thị tên CLB nhận

        public decimal TransferFee { get; set; }
        public int ContractYears { get; set; }

        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected"
    }
}
