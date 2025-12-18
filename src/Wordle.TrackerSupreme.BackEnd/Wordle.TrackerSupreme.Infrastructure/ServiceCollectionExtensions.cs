using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddWordleTrackerSupremeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
    {
        services.AddDbContext<WordleTrackerSupremeDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(nameof(WordleTrackerSupremeDbContext)), builder =>
            {
                builder.MigrationsAssembly(typeof(WordleTrackerSupremeDbContext).Assembly.FullName);
                builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                configureNpgsql?.Invoke(builder);
            });
        });
    }
}
