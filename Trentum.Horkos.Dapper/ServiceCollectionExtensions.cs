using Microsoft.Extensions.DependencyInjection;

namespace Trentum.Horkos;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHorkosDapper(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddScoped<IHorkosDictionaryService, HorkosDictionaryService>();
        return services;
    }
}
