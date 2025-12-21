using Microsoft.AspNetCore.Identity;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services.Game;

namespace Wordle.TrackerSupreme.Seeder;

public class SeedDataGenerator(
    SeederOptions options,
    GameOptions gameOptions,
    IWordSelector wordSelector,
    IGuessEvaluationService guessEvaluationService,
    PasswordHasher<Player> passwordHasher)
{
    private readonly SeederOptions _options = options;
    private readonly GameOptions _gameOptions = gameOptions;
    private readonly IWordSelector _wordSelector = wordSelector;
    private readonly IGuessEvaluationService _guessEvaluationService = guessEvaluationService;
    private readonly PasswordHasher<Player> _passwordHasher = passwordHasher;

    private static readonly string[] FirstNames =
    [
        "Ava", "Ben", "Cara", "Drew", "Eli", "Faye", "Gus", "Hana", "Ivy", "Jude",
        "Kai", "Lena", "Mara", "Nico", "Oren", "Pia", "Quinn", "Reid", "Sage", "Tess",
        "Uma", "Vale", "Wren", "Xena", "Yara", "Zane"
    ];

    private static readonly string[] LastNames =
    [
        "Stone", "Harper", "Brooks", "Lane", "Cruz", "Morgan", "Quill", "Rowe", "Blake", "Hart",
        "Wells", "Reed", "Hayes", "Parker", "Sloan", "Baker", "Carter", "Adler", "Shaw", "Bennett"
    ];

    public SeedData Generate(DateOnly anchorDate)
    {
        var random = new Random(_options.RandomSeed);
        var players = BuildPlayers(random, anchorDate);
        var puzzles = BuildPuzzles(anchorDate, random);
        var attempts = BuildAttempts(random, anchorDate, players, puzzles);

        return new SeedData(players, puzzles, attempts);
    }

    private List<Player> BuildPlayers(Random random, DateOnly anchorDate)
    {
        var players = new List<Player>(_options.PlayerCount);
        var createdOnBase = anchorDate.ToDateTime(TimeOnly.MinValue).AddDays(-60);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _options.PlayerCount; i++)
        {
            var displayName = BuildDisplayName(random, usedNames, i);
            var createdOn = createdOnBase.AddDays(random.Next(0, 60)).AddMinutes(random.Next(0, 600));

            var player = new Player
            {
                Id = Guid.NewGuid(),
                DisplayName = displayName,
                Email = $"{displayName.ToLowerInvariant()}@example.com",
                PasswordHash = string.Empty,
                CreatedOn = createdOn
            };

            player.PasswordHash = _passwordHasher.HashPassword(player, _options.DefaultPassword);
            players.Add(player);
        }

        return players;
    }

    private string BuildDisplayName(Random random, HashSet<string> usedNames, int index)
    {
        string displayName;
        do
        {
            var first = FirstNames[random.Next(FirstNames.Length)];
            var last = LastNames[random.Next(LastNames.Length)];
            displayName = $"{first}{last}{index + 1}";
        } while (!usedNames.Add(displayName));

        return displayName;
    }

    private List<DailyPuzzle> BuildPuzzles(DateOnly anchorDate, Random random)
    {
        var requiredDays = _options.MaxSolvedPuzzles + _options.FailedPuzzlesMax + _options.InProgressPuzzlesMax;
        var totalDays = Math.Max(_options.PuzzleDays, requiredDays);
        var puzzles = new List<DailyPuzzle>(totalDays);

        for (int i = 0; i < totalDays; i++)
        {
            var date = anchorDate.AddDays(-i);
            puzzles.Add(new DailyPuzzle
            {
                Id = Guid.NewGuid(),
                PuzzleDate = date,
                Solution = _wordSelector.GetSolutionFor(date),
                IsArchived = date < anchorDate
            });
        }

        puzzles.Reverse();
        return puzzles;
    }

    private List<PlayerPuzzleAttempt> BuildAttempts(
        Random random,
        DateOnly anchorDate,
        IReadOnlyList<Player> players,
        IReadOnlyList<DailyPuzzle> puzzles)
    {
        var attempts = new List<PlayerPuzzleAttempt>();
        var totalDays = puzzles.Count;
        var maxGuesses = _gameOptions.MaxGuesses;

        foreach (var player in players)
        {
            var solvedCount = random.Next(_options.MinSolvedPuzzles, _options.MaxSolvedPuzzles + 1);
            var failedCount = random.Next(_options.FailedPuzzlesMin, _options.FailedPuzzlesMax + 1);
            var inProgressCount = random.Next(_options.InProgressPuzzlesMin, _options.InProgressPuzzlesMax + 1);
            var totalCount = Math.Min(totalDays, solvedCount + failedCount + inProgressCount);

            var puzzleOrder = puzzles
                .OrderBy(_ => random.Next())
                .Take(totalCount)
                .ToList();

            var solved = puzzleOrder.Take(Math.Min(solvedCount, puzzleOrder.Count)).ToList();
            var remaining = puzzleOrder.Skip(solved.Count).ToList();
            var failed = remaining.Take(Math.Min(failedCount, remaining.Count)).ToList();
            var inProgress = remaining.Skip(failed.Count).Take(Math.Min(inProgressCount, remaining.Count - failed.Count)).ToList();

            foreach (var puzzle in solved)
            {
                attempts.Add(BuildSolvedAttempt(player, puzzle, random, maxGuesses));
            }

            foreach (var puzzle in failed)
            {
                attempts.Add(BuildFailedAttempt(player, puzzle, random, maxGuesses));
            }

            foreach (var puzzle in inProgress)
            {
                attempts.Add(BuildInProgressAttempt(player, puzzle, random, maxGuesses));
            }
        }

        return attempts;
    }

    private PlayerPuzzleAttempt BuildSolvedAttempt(Player player, DailyPuzzle puzzle, Random random, int maxGuesses)
    {
        var guessCount = random.Next(2, maxGuesses + 1);
        var createdOn = BuildAttemptCreatedOn(puzzle.PuzzleDate, random);
        var attemptId = Guid.NewGuid();
        var guesses = BuildGuessAttempts(attemptId, puzzle, random, guessCount, solved: true);

        var attempt = new PlayerPuzzleAttempt
        {
            Id = attemptId,
            PlayerId = player.Id,
            Player = player,
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.Solved,
            CreatedOn = createdOn,
            CompletedOn = createdOn.AddMinutes(random.Next(8, 40)),
            PlayedInHardMode = random.NextDouble() < 0.35,
            Guesses = guesses
        };

        AttachAttemptReferences(attempt);
        return attempt;
    }

    private PlayerPuzzleAttempt BuildFailedAttempt(Player player, DailyPuzzle puzzle, Random random, int maxGuesses)
    {
        var createdOn = BuildAttemptCreatedOn(puzzle.PuzzleDate, random);
        var attemptId = Guid.NewGuid();
        var guesses = BuildGuessAttempts(attemptId, puzzle, random, maxGuesses, solved: false);

        var attempt = new PlayerPuzzleAttempt
        {
            Id = attemptId,
            PlayerId = player.Id,
            Player = player,
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.Failed,
            CreatedOn = createdOn,
            CompletedOn = createdOn.AddMinutes(random.Next(20, 55)),
            PlayedInHardMode = random.NextDouble() < 0.2,
            Guesses = guesses
        };

        AttachAttemptReferences(attempt);
        return attempt;
    }

    private PlayerPuzzleAttempt BuildInProgressAttempt(Player player, DailyPuzzle puzzle, Random random, int maxGuesses)
    {
        var guessCount = random.Next(1, Math.Max(2, maxGuesses));
        var createdOn = BuildAttemptCreatedOn(puzzle.PuzzleDate, random);
        var attemptId = Guid.NewGuid();
        var guesses = BuildGuessAttempts(attemptId, puzzle, random, guessCount, solved: false);

        var attempt = new PlayerPuzzleAttempt
        {
            Id = attemptId,
            PlayerId = player.Id,
            Player = player,
            DailyPuzzleId = puzzle.Id,
            DailyPuzzle = puzzle,
            Status = AttemptStatus.InProgress,
            CreatedOn = createdOn,
            CompletedOn = null,
            PlayedInHardMode = random.NextDouble() < 0.15,
            Guesses = guesses
        };

        AttachAttemptReferences(attempt);
        return attempt;
    }

    private List<GuessAttempt> BuildGuessAttempts(Guid attemptId, DailyPuzzle puzzle, Random random, int count, bool solved)
    {
        if (string.IsNullOrWhiteSpace(puzzle.Solution))
        {
            throw new InvalidOperationException("Seed puzzle solution is required.");
        }

        var guesses = new List<GuessAttempt>(count);
        for (int i = 0; i < count; i++)
        {
            var isLast = i == count - 1;
            var guessWord = isLast && solved
                ? puzzle.Solution
                : GenerateGuessWord(random, puzzle.Solution);

            var normalized = _guessEvaluationService.NormalizeGuess(guessWord);
            var feedback = _guessEvaluationService.EvaluateGuess(puzzle.Solution, normalized).ToList();

            var guessAttempt = new GuessAttempt
            {
                Id = Guid.NewGuid(),
                PlayerPuzzleAttemptId = attemptId,
                GuessNumber = i + 1,
                GuessWord = normalized,
                Feedback = feedback
            };

            foreach (var evaluation in feedback)
            {
                evaluation.GuessAttemptId = guessAttempt.Id;
                evaluation.GuessAttempt = guessAttempt;
            }

            guesses.Add(guessAttempt);
        }

        return guesses;
    }

    private string GenerateGuessWord(Random random, string solution)
    {
        var letters = new char[_gameOptions.WordLength];
        string guessWord;

        do
        {
            for (int i = 0; i < letters.Length; i++)
            {
                letters[i] = (char)('A' + random.Next(0, 26));
            }

            guessWord = new string(letters);
        } while (guessWord.Equals(solution, StringComparison.OrdinalIgnoreCase));

        return guessWord;
    }

    private static DateTime BuildAttemptCreatedOn(DateOnly puzzleDate, Random random)
    {
        var baseTime = puzzleDate.ToDateTime(TimeOnly.MinValue).AddHours(7);
        return baseTime.AddMinutes(random.Next(0, 600));
    }

    private static void AttachAttemptReferences(PlayerPuzzleAttempt attempt)
    {
        foreach (var guess in attempt.Guesses)
        {
            guess.PlayerPuzzleAttempt = attempt;
            guess.PlayerPuzzleAttemptId = attempt.Id;
        }
    }
}

public record SeedData(
    IReadOnlyList<Player> Players,
    IReadOnlyList<DailyPuzzle> Puzzles,
    IReadOnlyList<PlayerPuzzleAttempt> Attempts);
