using Application.Services.Abstract;
using Application.Services.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extenstion;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAnimeService, AnimeService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
