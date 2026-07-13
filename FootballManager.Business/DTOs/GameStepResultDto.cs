using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.DTOs
{
    public class GameStepResultDto
    {
        public int FinishedWeek { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PlayedMatchesCount { get; set; }
    }
}
