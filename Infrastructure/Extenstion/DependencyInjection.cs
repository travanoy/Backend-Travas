using Application.Interfaces;
using Application.Services.Abstract;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.External;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Extenstion;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IAnimeRepository, AnimeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProgressRepository, ProgressRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();

        services.AddScoped<IPasswordManager, PasswordManager>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<ShikimoriOptions>(configuration.GetSection("Shikimori"));
        services.Configure<KodikOptions>(configuration.GetSection("Kodik"));

        services.AddHttpClient<IShikimoriClient, ShikimoriClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ShikimoriOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IKodikClient, KodikClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<KodikOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IAnilibriaClient, AnilibriaClient>(client =>
        {
            client.BaseAddress = new Uri("https://anilibria.top/api/v1/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TravasApp/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IPushNotificationService, ExpoPushService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHostedService<DailyAnimeSyncBackgroundService>();
        services.AddHostedService<EpisodeCheckerBackgroundService>();

        return services;
    }
}
