namespace FootballManager.Data.Entities
{
    // Kho Tên theo quốc gia, dùng để sinh ngẫu nhiên tên cầu thủ trẻ đôn lên cuối mùa
    public class PlayerGivenName
    {
        public int Id { get; set; }
        public string Nation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
