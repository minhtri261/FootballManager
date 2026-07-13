using FootballManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.Services.Interfaces
{
    public interface ISeasonService : IBaseService<SeasonSummary>
    {
        Task CompleteSeasonAsync(int seasonNumber);
    }
}
