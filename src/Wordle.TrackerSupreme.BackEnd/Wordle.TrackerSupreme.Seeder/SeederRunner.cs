using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Seeder;

public class SeederRunner(
    WordleTrackerSupremeDbContext dbContext,
    SeederOptions options,
    SeedDataGenerator generator,
    ILogger<SeederRunner> logger)
{
    private readonly WordleTrackerSupremeDbContext _dbContext = dbContext;
    private readonly SeederOptions _options = options;
    private readonly SeedDataGenerator _generator = generator;
    private readonly ILogger<SeederRunner> _logger = logger;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        if (!_options.AllowReseed && await HasExistingData(cancellationToken))
        {
            _logger.LogInformation("Database already contains data; skipping seed.");
            return;
        }

        _logger.LogInformation(
            "Seeder options: PlayerCount={PlayerCount}, PuzzleDays={PuzzleDays}, SolvedRange={MinSolved}-{MaxSolved}, FailedRange={FailedMin}-{FailedMax}, InProgressRange={InProgressMin}-{InProgressMax}, AnchorDate={AnchorDate}.",
            _options.PlayerCount,
            _options.PuzzleDays,
            _options.MinSolvedPuzzles,
            _options.MaxSolvedPuzzles,
            _options.FailedPuzzlesMin,
            _options.FailedPuzzlesMax,
            _options.InProgressPuzzlesMin,
            _options.InProgressPuzzlesMax,
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
