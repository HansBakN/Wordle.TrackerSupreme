using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Seeder;

public class SeederRunner(
    WordleTrackerSupremeDbContext dbContext,
    SeederOptions options,
    SeedDataGenerator generator,
    ILogger<SeederRunner> logger,
    IHostEnvironment environment)
{
    private readonly WordleTrackerSupremeDbContext _dbContext = dbContext;
    private readonly SeederOptions _options = options;
    private readonly SeedDataGenerator _generator = generator;
    private readonly ILogger<SeederRunner> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        if (_options.ResetDatabase)
        {
            EnsureResetAllowed();
            await ResetDatabaseAsync(cancellationToken);
        }

        if (!_options.ResetDatabase && !_options.AllowReseed && await HasExistingData(cancellationToken))
        {
            _logger.LogInformation("Database already contains data; skipping seed.");
            return;
        }

        _logger.LogInformation(
            "Seeder options: PlayerCount={PlayerCount}, PuzzleDays={PuzzleDays}, SolvedRange={MinSolved}-{MaxSolved}, FailedRange={FailedMin}-{FailedMax}, InProgressRange={InProgressMin}-{InProgressMax}, AllowReseed={AllowReseed}, ResetDatabase={ResetDatabase}, AnchorDate={AnchorDate}.",
            _options.PlayerCount,
            _options.PuzzleDays,
            _options.MinSolvedPuzzles,
            _options.MaxSolvedPuzzles,
            _options.FailedPuzzlesMin,
            _options.FailedPuzzlesMax,
            _options.InProgressPuzzlesMin,
            _options.InProgressPuzzlesMax,
            _options.AllowReseed,
            _options.ResetDatabase,
            _options.AnchorDate ?? "(auto)");

        var anchorDate = ResolveAnchorDate();
        var data = _generator.Generate(anchorDate);

        _dbContext.Players.AddRange(data.Players);
        _dbContext.DailyPuzzles.AddRange(data.Puzzles);
        _dbContext.Attempts.AddRange(data.Attempts);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded {PlayerCount} players, {PuzzleCount} puzzles, and {AttemptCount} attempts.",
            data.Players.Count,
            data.Puzzles.Count,
            data.Attempts.Count);
    }

    private void ValidateOptions()
    {
        if (_options.MinSolvedPuzzles > _options.MaxSolvedPuzzles)
        {
            throw new InvalidOperationException("SeederOptions MinSolvedPuzzles cannot exceed MaxSolvedPuzzles.");
        }

        if (_options.FailedPuzzlesMin > _options.FailedPuzzlesMax)
        {
            throw new InvalidOperationException("SeederOptions FailedPuzzlesMin cannot exceed FailedPuzzlesMax.");
        }

        if (_options.InProgressPuzzlesMin > _options.InProgressPuzzlesMax)
        {
            throw new InvalidOperationException("SeederOptions InProgressPuzzlesMin cannot exceed InProgressPuzzlesMax.");
        }

        if (_options.PlayerCount <= 0)
        {
            throw new InvalidOperationException("SeederOptions PlayerCount must be greater than zero.");
        }
    }

    private void EnsureResetAllowed()
    {
        if (_environment.IsProduction())
        {
            throw new InvalidOperationException("Resetting seed data is not allowed in Production.");
        }

        var resetEnabled = Environment.GetEnvironmentVariable("E2E_RESET_ENABLED");
        if (!string.Equals(resetEnabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ResetDatabase requested but E2E_RESET_ENABLED is not set to true.");
        }
    }

    private async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resetting database before seeding.");

        if (string.Equals(
            _dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.InMemory",
            StringComparison.Ordinal))
        {
            await _dbContext.Database.EnsureDeletedAsync(cancellationToken);
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return;
        }

        await _dbContext.LetterEvaluations.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Guesses.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Attempts.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.DailyPuzzles.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Players.ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<bool> HasExistingData(CancellationToken cancellationToken)
    {
        var hasPlayers = await _dbContext.Players.AnyAsync(cancellationToken);
        if (hasPlayers)
        {
            return true;
        }

        return await _dbContext.DailyPuzzles.AnyAsync(cancellationToken);
    }

    private DateOnly ResolveAnchorDate()
    {
        if (!string.IsNullOrWhiteSpace(_options.AnchorDate)
            && DateOnly.TryParse(_options.AnchorDate, out var parsed))
        {
            return parsed;
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
