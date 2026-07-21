using FootballManager.Data.Entities;

namespace FootballManager.Business.Helpers
{
    public static class TransferCalculator
    {
        /// <summary>
        /// Tính giá chuyển nhượng dựa trên Quality, dư địa phát triển (Potential), độ tuổi (PlayerLifeCycle)
        /// và số năm hợp đồng còn lại.
        /// </summary>
        public static decimal CalculateTransferPrice(Footballer player)
        {
            double baseValue = Math.Pow(player.Quality, 1.5);
            double potentialBonus = 1 + Math.Max(0, player.Potential - player.Quality) / 50.0;
            double ageFactor = player.Status switch
            {
                PlayerLifeCycle.Youth => 1.3,
                PlayerLifeCycle.Rising => 1.2,
                PlayerLifeCycle.Peak => 1.0,
                PlayerLifeCycle.Stable => 0.7,
                _ => 0.4 // Veteran/Retired
            };
            double contractFactor = 0.4 + 0.15 * player.ContractYears;

            return (decimal)(baseValue * potentialBonus * ageFactor * contractFactor);
        }

        /// <summary>
        /// Tính score của CLB cho cầu thủ
        /// </summary>
        public static double CalculateClubScore(Footballer player, Club club)
        {
            double score = 0;

            double avgQuality = club.Footballers.Any()
                ? club.Footballers.Average(f => f.Quality)
                : player.Quality;

            score += avgQuality * 0.4; // Chất lượng trung bình đội

            score += (club.LeagueCups + club.NationalCups + club.ChampionsCups) * 0.2; // danh hiệu

            score += (double)club.Money / 100 * 1.5; // tài chính

            if (player.ClubId == club.Id)
                score += 0.2; // Retention Factor: CLB chủ quản muốn giữ

            return score;
        }

        /// <summary>
        /// Chuyển score thành % để chọn CLB ngẫu nhiên
        /// </summary>
        public static Dictionary<Club, double> ConvertScoreToPercent(Dictionary<Club, double> scores)
        {
            double total = scores.Values.Sum();
            return scores.ToDictionary(kv => kv.Key, kv => kv.Value / total * 100);
        }

        /// <summary>
        /// Chọn CLB dựa trên xác suất %
        /// </summary>
        public static Club ChooseClubByRandom(Dictionary<Club, double> percentages, Random rand)
        {
            double randomValue = rand.NextDouble() * 100;
            double cumulative = 0;
            foreach (var kv in percentages)
            {
                cumulative += kv.Value;
                if (randomValue <= cumulative)
                    return kv.Key;
            }
            return percentages.Keys.First(); // fallback
        }
    }
}
