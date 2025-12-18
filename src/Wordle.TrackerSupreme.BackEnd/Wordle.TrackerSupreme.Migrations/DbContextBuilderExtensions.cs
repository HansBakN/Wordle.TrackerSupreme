using System.Reflection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Wordle.TrackerSupreme.Migrations;

public static class DbContextBuilderExtensions
{
    public static NpgsqlDbContextOptionsBuilder AllowMigrationManagement(this NpgsqlDbContextOptionsBuilder builder)
        => builder.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
}
