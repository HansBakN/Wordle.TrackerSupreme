using System.Security.Cryptography;
using System.Text;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Domain.Services.Import;

namespace Wordle.TrackerSupreme.Application.Services.Import;

public class NytImportService(
    INytImportRepository importRepository,
    IGameRepository gameRepository,
    IOfficialWordProvider officialWordProvider,
    IGuessEvaluationService guessEvaluationService,
    NytPuzzleCatalogue catalogue,
    NytImportOptions options,
    TimeProvider timeProvider) : INytImportService
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<NytImportSessionCreated> CreateSession(Guid playerId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var code = CreateCode();
        var session = new NytImportSession
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CodeHash = HashCode(code),
            CreatedOn = now,
            ExpiresAt = now.AddMinutes(options.SessionLifetimeMinutes)
        };
        await importRepository.AddSession(session, cancellationToken);
        await importRepository.SaveChanges(cancellationToken);
        return new NytImportSessionCreated(session.Id, code, session.ExpiresAt);
    }

    public async Task<NytImportSessionStatus?> GetSession(Guid playerId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await importRepository.GetSession(sessionId, cancellationToken);
        return session is null || session.PlayerId != playerId ? null : MapStatus(session);
    }

    public async Task<IReadOnlyList<NytPuzzleMetadata>> GetPublishedCatalogue(string importCode, CancellationToken cancellationToken)
    {
        var session = await importRepository.GetSessionByCodeHash(HashCode(importCode), cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (session is null || session.UsedAt is not null || session.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException("The import code is invalid or expired.");
        }
        return catalogue.Published;
    }

    public async Task<NytImportResult> Import(NytImportRequest request, CancellationToken cancellationToken)
    {
        if (request.SchemaVersion != 1)
        {
            throw new ArgumentException("Unsupported import schema version.");
        }

        if (request.States.Count > options.MaxStates)
        {
            throw new ArgumentException($"An import may contain at most {options.MaxStates} states.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var session = await importRepository.GetSessionByCodeHash(HashCode(request.ImportCode), cancellationToken)
            ?? throw new UnauthorizedAccessException("The import code is invalid.");
        if (session.UsedAt is not null || session.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException(session.UsedAt is not null
                ? "The import code has already been used." : "The import code has expired.");
        }

        if (!await importRepository.TryClaimSession(session.Id, now, cancellationToken))
        {
            throw new UnauthorizedAccessException("The import code has already been used.");
        }

        session.UsedAt = now;
        var results = new List<NytImportRecordResult>();
        foreach (var state in request.States)
        {
            results.Add(await ImportState(session, state, now, cancellationToken));
        }

        session.AggregateGamesPlayed = request.AggregateGamesPlayed;
        session.Requested = request.States.Count;
        session.Imported = results.Count(result => result.Outcome == "imported");
        session.Duplicates = results.Count(result => result.Outcome == "duplicate");
        session.Conflicts = results.Count(result => result.Outcome == "conflict");
        session.Rejected = results.Count(result => result.Outcome == "rejected");
        session.MissingFromNyt = Math.Max(0, (request.AggregateGamesPlayed ?? request.States.Count) - request.States.Count);
        session.CompletedAt = timeProvider.GetUtcNow().UtcDateTime;
        await importRepository.SaveChanges(cancellationToken);
        return new NytImportResult(session.Requested, session.Imported, session.Duplicates, session.Conflicts,
            session.Rejected, session.MissingFromNyt, results);
    }

    private async Task<NytImportRecordResult> ImportState(NytImportSession session, NytImportedState? state, DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            if (state is null)
            {
                return Reject(0, default, "The import record is malformed.");
            }

            if (state.NytPuzzleId is null || state.PrintDate is null)
            {
                return Reject(state, "The puzzle ID and print date are required.");
            }

            var nytPuzzleId = state.NytPuzzleId.Value;
            var printDate = state.PrintDate.Value;

            if (!catalogue.TryGet(nytPuzzleId, out var metadata) || metadata.PrintDate != printDate)
            {
                return Reject(state, "The puzzle ID and print date are not in the published NYT catalogue.");
            }

            if (printDate > DateOnly.FromDateTime(now))
            {
                return Reject(state, "Future puzzles cannot be imported.");
            }

            var status = (state.Status ?? string.Empty).ToUpperInvariant() switch
            {
                "WIN" => AttemptStatus.Solved,
                "FAIL" => AttemptStatus.Failed,
                _ => throw new ArgumentException("Only completed WIN or FAIL states can be imported.")
            };
            if (state.BoardState is null)
            {
                throw new ArgumentException("A completed game must include a board state.");
            }

            var boardState = state.BoardState;
            if (boardState.Count is < 1 or > 6)
            {
                throw new ArgumentException("A completed game must contain between one and six guesses.");
            }

            var guesses = boardState.Select(guess => guessEvaluationService.NormalizeGuess(guess ?? string.Empty)).ToList();
            var official = await officialWordProvider.GetMetadataForDateAsync(printDate, cancellationToken);
            if (official.NytPuzzleId != nytPuzzleId)
            {
                throw new ArgumentException("The official NYT puzzle metadata no longer matches the catalogue.");
            }

            var solution = official.Solution ?? throw new InvalidOperationException("The official solution is unavailable.");
            if (status == AttemptStatus.Solved && (guesses[^1] != solution || guesses.Take(guesses.Count - 1).Contains(solution)))
            {
                throw new ArgumentException("A winning game must end with the official solution and cannot continue after solving.");
            }

            if (status == AttemptStatus.Failed && (guesses.Count != 6 || guesses.Contains(solution)))
            {
                throw new ArgumentException("A failed game must contain six guesses and must not contain the solution.");
            }

            var puzzle = await gameRepository.GetPuzzleByDate(printDate, PuzzleStream.NewYorkTimes, cancellationToken);
            if (puzzle is null)
            {
                puzzle = new DailyPuzzle
                {
                    Id = Guid.NewGuid(),
                    PuzzleDate = printDate,
                    Stream = PuzzleStream.NewYorkTimes,
                    Solution = solution,
                    NytPuzzleId = metadata.NytPuzzleId,
                    PublicPuzzleNumber = metadata.PublicPuzzleNumber
                };
                await gameRepository.AddPuzzle(puzzle, cancellationToken);
            }

            var existing = await gameRepository.GetAttempt(session.PlayerId, puzzle.Id, cancellationToken);
            if (existing is not null)
            {
                var existingGuesses = existing.Guesses.OrderBy(guess => guess.GuessNumber).Select(guess => guess.GuessWord).ToList();
                var same = existing.Status == status && existingGuesses.SequenceEqual(guesses);
                return new NytImportRecordResult(nytPuzzleId, printDate, same ? "duplicate" : "conflict");
            }

            var sourceTime = ParseTimestamp(state.Timestamp, printDate, state.IsPlayingArchive, now);
            var attempt = new PlayerPuzzleAttempt
            {
                Id = Guid.NewGuid(),
                PlayerId = session.PlayerId,
                DailyPuzzleId = puzzle.Id,
                DailyPuzzle = puzzle,
                Status = status,
                PlayedInHardMode = state.HardMode,
                IsImportedArchive = state.IsPlayingArchive,
                CreatedOn = sourceTime,
                CompletedOn = sourceTime,
                NytImportSessionId = session.Id
            };
            await gameRepository.AddAttempt(attempt, cancellationToken);
            for (var index = 0; index < guesses.Count; index++)
            {
                var guess = new GuessAttempt
                {
                    Id = Guid.NewGuid(),
                    PlayerPuzzleAttemptId = attempt.Id,
                    GuessNumber = index + 1,
                    GuessWord = guesses[index]
                };
                var feedback = guessEvaluationService.EvaluateGuess(solution, guesses[index]);
                foreach (var evaluation in feedback)
                {
                    evaluation.GuessAttemptId = guess.Id;
                }
                await gameRepository.AddGuess(guess, feedback, cancellationToken);
            }

            await gameRepository.SaveChanges(cancellationToken);
            return new NytImportRecordResult(nytPuzzleId, printDate, "imported");
        }
        catch (ArgumentException exception)
        {
            return Reject(state, exception.Message);
        }
    }

    private static NytImportRecordResult Reject(NytImportedState? state, string reason)
        => Reject(state?.NytPuzzleId ?? 0, state?.PrintDate ?? default, reason);

    private static NytImportRecordResult Reject(int nytPuzzleId, DateOnly printDate, string reason)
        => new(nytPuzzleId, printDate, "rejected", reason);

    private static DateTime ParseTimestamp(long? timestamp, DateOnly printDate, bool isPlayingArchive, DateTime now)
    {
        if (timestamp is not null)
        {
            try
            {
                var value = timestamp > 10_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value) : DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
                if (value.UtcDateTime <= now.AddMinutes(5) && DateOnly.FromDateTime(value.UtcDateTime) >= printDate)
                {
                    return value.UtcDateTime;
                }
            }
            catch (ArgumentOutOfRangeException) { }
        }
        return isPlayingArchive ? now : printDate.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
    }

    private static string CreateCode()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var chars = bytes.ToArray().Select(value => CodeAlphabet[value % CodeAlphabet.Length]).ToArray();
        return $"{new string(chars[..4])}-{new string(chars[4..])}";
    }

    private static string HashCode(string code)
    {
        var normalized = new string((code ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static NytImportSessionStatus MapStatus(NytImportSession session)
        => new(session.Id, session.ExpiresAt, session.CompletedAt, session.AggregateGamesPlayed, session.Requested,
            session.Imported, session.Duplicates, session.Conflicts, session.Rejected, session.MissingFromNyt);
}
