using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
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
    private readonly bool _useOfficialWords;

    public DailyPuzzleService(
        IGameRepository gameRepository,
        IWordSelector wordSelector,
        IOfficialWordProvider officialWordProvider,
        IHostEnvironment hostEnvironment,
        ILogger<DailyPuzzleService> logger)
    {
        _gameRepository = gameRepository;
        _wordSelector = wordSelector;
        _officialWordProvider = officialWordProvider;
        _logger = logger;
        _useOfficialWords = !hostEnvironment.IsDevelopment();
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
        if (_useOfficialWords)
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
                _logger.LogWarning(ex, "Failed to retrieve the official solution for {PuzzleDate}; using the deterministic dictionary instead.", puzzleDate);
            }
        }

        return _wordSelector.GetSolutionFor(puzzleDate);
    }
}
