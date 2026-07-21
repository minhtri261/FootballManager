namespace FootballManager.Data.Entities
{
    // Kho Họ theo quốc gia, dùng để sinh ngẫu nhiên tên cầu thủ trẻ đôn lên cuối mùa
    public class PlayerSurname
    {
        public int Id { get; set; }
        public string Nation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
