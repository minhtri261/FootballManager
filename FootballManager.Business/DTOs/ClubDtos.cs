
namespace FootballManager.Business.DTOs
{
    public class ClubCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Nation { get; set; } = string.Empty;
        public decimal Money { get; set; }
        public int LeagueCups { get; set; }
        public int ChampionsCups { get; set; }
        public int NationalCups { get; set; }
        public int TrainingQuality { get; set; }
        public bool IsBot { get; set; }
    }

    public class ClubUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Nation { get; set; } = string.Empty;
        public decimal Money { get; set; }
        public int LeagueCups { get; set; }
        public int ChampionsCups { get; set; }
        public int NationalCups { get; set; }
        public int TrainingQuality { get; set; }
        public bool IsBot { get; set; }
    }
}

