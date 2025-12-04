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
            services.AddScoped(typeof(IGenericRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

            // ✅ Cụ thể từng entity ở dưới này
            // Repos
            services.AddScoped<IFootballerRepository, FootballerRepository>();
            services.AddScoped<IClubRepository, ClubRepository>();
            services.AddScoped<ITournamentRepository, TournamentRepository>();
            services.AddScoped<IMatchRepository, MatchRepository>();
            services.AddScoped<IGameStateRepository, GameStateRepository>();
            services.AddScoped<ITransferRepository, TransferRepository>();
            services.AddScoped<IMatchLineupRepository, MatchLineupRepository>();
            services.AddScoped<IMatchStatsRepository, MatchStatsRepository>();

            // Services
            services.AddScoped<IFootballerService, FootballerService>();
            services.AddScoped<IClubService, ClubService>();
            services.AddScoped<ITournamentService, TournamentService>();
            services.AddScoped<TournamentClubService>();
            services.AddScoped<TournamentMatchService>();
            services.AddScoped<IGameStateService, GameStateService>();
            services.AddScoped<ITransferService, TransferService>();
            services.AddScoped<IBotLineupService, BotLineupService>();
            services.AddScoped<IMatchService, MatchService>();
            services.AddScoped<RandomResultService>();

            return services;
        }
    }
}
