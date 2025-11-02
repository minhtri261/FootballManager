namespace FootballManagerMVC.Models
{
    public class Club
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Nation { get; set; }
        public decimal Money { get; set; }
        public int LeagueCups { get; set; }
        public int ChampionsCups { get; set; }
        public int NationalCups { get; set; }
        public int TrainingQuality { get; set; }
        public bool IsBot { get; set; }
    }

}
