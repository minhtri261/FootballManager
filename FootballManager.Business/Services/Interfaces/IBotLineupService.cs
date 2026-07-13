using FootballManager.Data.Entities;

namespace FootballManager.Business.Services.Interfaces
{
    public interface IBotLineupService
    {
        Task SetupBotLineupsForSeasonAsync(int seasonNumber);
    }

}
