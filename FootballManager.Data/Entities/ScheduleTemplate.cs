using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Data.Entities
{
    public class ScheduleTemplate
    {
        public int Id { get; set; }
        public int Week { get; set; }
        public TournamentType? TournamentType { get; set; } // Null nếu là tuần nghỉ hoặc chỉ chuyển nhượng
        public int Round { get; set; }
        public string Description { get; set; }

        // Thêm các thuộc tính điều hướng logic
        public bool IsTransferOpen { get; set; }   // Tuần này có cho phép mua bán không?
        public bool IsTrainingBoost { get; set; }  // Tuần nghỉ, cầu thủ được tăng chỉ số nhiều hơn?
        public bool IsSeasonEnd { get; set; }      // Đánh dấu tuần 51 để chạy logic tổng kết mùa
    }
}
