using FootballManager.Business.Services;
using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Repositories;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManager.Business
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

            // ✅ Cụ thể từng entity ở dưới này
            // Repos
            services.AddScoped<ITournamentRepository, TournamentRepository>();
            services.AddScoped<IMatchRepository, MatchRepository>();
            services.AddScoped<IGameStateRepository, GameStateRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IClubRepository, ClubRepository>();
            services.AddScoped<IFootballerRepository, FootballerRepository>();
            services.AddScoped<ITransferRepository, TransferRepository>();

            // Services
            services.AddScoped<IGameStateService, GameStateService>();
            services.AddScoped<IBotLineupService, BotService>();
            services.AddScoped<ITransferService, TransferService>();
            services.AddScoped<ISeasonService, SeasonService>();
            services.AddScoped<IMyClubService, MyClubService>();
            services.AddScoped<ITournamentService, TournamentService>();
            return services;
        }
    }
}
