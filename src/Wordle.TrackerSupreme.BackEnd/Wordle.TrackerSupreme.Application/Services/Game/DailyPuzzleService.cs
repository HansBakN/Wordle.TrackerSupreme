using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Application.Services.Game;

public class DailyPuzzleService : IDailyPuzzleService
{
    private readonly IGameRepository _gameRepository;
    private readonly IWordSelector _wordSelector;
    private readonly IOfficialWordProvider _officialWordProvider;
    private readonly ILogger<DailyPuzzleService> _logger;

    public DailyPuzzleService(
        IGameRepository gameRepository,
        IWordSelector wordSelector,
        IOfficialWordProvider officialWordProvider,
        ILogger<DailyPuzzleService> logger)
    {
        _gameRepository = gameRepository;
        _wordSelector = wordSelector;
        _officialWordProvider = officialWordProvider;
        _logger = logger;
    }

    public async Task<DailyPuzzle> GetOrCreatePuzzle(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        var existing = await _gameRepository.GetPuzzleByDate(puzzleDate, cancellationToken);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.Solution))
            {
                existing.Solution = await ResolveSolution(puzzleDate, cancellationToken);
                await _gameRepository.SaveChanges(cancellationToken);
            }

            return existing;
        }

        var puzzle = new DailyPuzzle
        {
            Id = Guid.NewGuid(),
            PuzzleDate = puzzleDate,
            Solution = await ResolveSolution(puzzleDate, cancellationToken),
            IsArchived = false
        };

        await _gameRepository.AddPuzzle(puzzle, cancellationToken);
        await _gameRepository.SaveChanges(cancellationToken);
        return puzzle;
    }

    private async Task<string> ResolveSolution(DateOnly puzzleDate, CancellationToken cancellationToken)
    {
        try
        {
            return await _officialWordProvider.GetSolutionForDateAsync(puzzleDate, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve the official solution for {PuzzleDate}.", puzzleDate);
            throw new DailyPuzzleUnavailableException("Unable to retrieve today's puzzle. Please try again later.", ex);
        }
    }
}
