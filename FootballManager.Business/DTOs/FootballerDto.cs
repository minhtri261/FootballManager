using FootballManager.Data.Entities;

namespace FootballManager.Business.DTOs
{
    public class FootballerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Nation { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int Quality { get; set; }
        public int ContractYears { get; set; }
        public PlayerLifeCycle Status { get; set; } = PlayerLifeCycle.Youth;
        public bool IsTransferListed { get; set; }

        public int? ClubId { get; set; }
        public string? ClubName { get; set; }

        public int AwardQBV { get; set; }
        public int AwardQBB { get; set; }
        public int AwardQBD { get; set; }
    }
}
