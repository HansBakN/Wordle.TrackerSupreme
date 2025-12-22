using Microsoft.AspNetCore.Identity;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Domain.Services;

namespace Wordle.TrackerSupreme.Application.Services.Admin;

public class AdminService(
    IPlayerRepository playerRepository,
    IGameRepository gameRepository,
    IGuessEvaluationService guessEvaluationService,
    PasswordHasher<Player> passwordHasher,
    GameOptions options) : IAdminService
{
    private readonly IPlayerRepository _playerRepository = playerRepository;
    private readonly IGameRepository _gameRepository = gameRepository;
    private readonly IGuessEvaluationService _guessEvaluationService = guessEvaluationService;
    private readonly PasswordHasher<Player> _passwordHasher = passwordHasher;
    private readonly GameOptions _options = options;

    public async Task<IReadOnlyList<Player>> GetPlayers(CancellationToken cancellationToken)
        => await _playerRepository.GetPlayersWithAttempts(cancellationToken);

    public Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken)
        => _playerRepository.GetPlayerWithAttemptsAndGuesses(playerId, cancellationToken);

    public async Task<Player> UpdatePlayerProfile(Guid playerId, string displayName, string email, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetPlayer(playerId, cancellationToken);
        if (player is null)
        {
            throw new KeyNotFoundException("Player not found.");
        }

        var trimmedName = displayName.Trim();
        var trimmedEmail = email.Trim().ToLowerInvariant();

        var nameTaken = await _playerRepository.IsDisplayNameTaken(trimmedName, playerId, cancellationToken);
        if (nameTaken)
        {
            throw new InvalidOperationException("Display name is already taken.");
        }

        var emailTaken = await _playerRepository.IsEmailTaken(trimmedEmail, playerId, cancellationToken);
        if (emailTaken)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        player.DisplayName = trimmedName;
        player.Email = trimmedEmail;

        await _playerRepository.SaveChanges(cancellationToken);
        return player;
    }

    public async Task<Player> ResetPassword(Guid playerId, string newPassword, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetPlayer(playerId, cancellationToken);
        if (player is null)
        {
            throw new KeyNotFoundException("Player not found.");
        }

        player.PasswordHash = _passwordHasher.HashPassword(player, newPassword);
        await _playerRepository.SaveChanges(cancellationToken);
        return player;
    }

    public async Task<Player> SetAdminStatus(Guid playerId, bool isAdmin, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetPlayer(playerId, cancellationToken);
        if (player is null)
        {
            throw new KeyNotFoundException("Player not found.");
        }

        player.IsAdmin = isAdmin;
        await _playerRepository.SaveChanges(cancellationToken);
        return player;
    }

    public async Task<PlayerPuzzleAttempt> UpdateAttempt(
        Guid attemptId,
        IReadOnlyList<string> guesses,
        bool playedInHardMode,
        CancellationToken cancellationToken)
    {
        var attempt = await _gameRepository.GetAttemptWithDetails(attemptId, cancellationToken);
        if (attempt is null)
        {
            throw new KeyNotFoundException("Attempt not found.");
        }

        var solution = attempt.DailyPuzzle.Solution;
        if (string.IsNullOrWhiteSpace(solution))
        {
            throw new InvalidOperationException("Attempt does not have a solution to evaluate guesses against.");
        }

        if (guesses.Count > _options.MaxGuesses)
        {
            throw new ArgumentException($"Guesses cannot exceed {_options.MaxGuesses}.", nameof(guesses));
        }

        var normalizedGuesses = guesses
            .Select(guess => _guessEvaluationService.NormalizeGuess(guess))
            .ToList();

        await _gameRepository.RemoveGuesses(attempt.Guesses.ToList(), cancellationToken);
        attempt.Guesses.Clear();

        foreach (var (guessWord, index) in normalizedGuesses.Select((word, idx) => (word, idx)))
        {
            var feedback = _guessEvaluationService.EvaluateGuess(solution, guessWord).ToList();
            var guessAttempt = new GuessAttempt
            {
                Id = Guid.NewGuid(),
                PlayerPuzzleAttemptId = attempt.Id,
                GuessNumber = index + 1,
                GuessWord = guessWord,
                Feedback = feedback
            };

            foreach (var entry in feedback)
            {
                entry.GuessAttemptId = guessAttempt.Id;
                entry.GuessAttempt = guessAttempt;
            }

            attempt.Guesses.Add(guessAttempt);
            await _gameRepository.AddGuess(guessAttempt, feedback, cancellationToken);
        }

        attempt.PlayedInHardMode = playedInHardMode;
        UpdateAttemptStatus(attempt, normalizedGuesses, solution);

        await _gameRepository.SaveChanges(cancellationToken);
        return attempt;
    }

    public async Task DeleteAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _gameRepository.GetAttemptWithDetails(attemptId, cancellationToken);
        if (attempt is null)
        {
            throw new KeyNotFoundException("Attempt not found.");
        }

        await _gameRepository.RemoveGuesses(attempt.Guesses.ToList(), cancellationToken);
        await _gameRepository.RemoveAttempt(attempt, cancellationToken);
        await _gameRepository.SaveChanges(cancellationToken);
    }

    private void UpdateAttemptStatus(PlayerPuzzleAttempt attempt, IReadOnlyList<string> guesses, string solution)
    {
        if (guesses.Count == 0)
        {
            attempt.Status = AttemptStatus.InProgress;
            attempt.CompletedOn = null;
            return;
        }

        var isSolved = guesses.Any(guess =>
            _guessEvaluationService.EvaluateGuess(solution, guess).All(entry => entry.Result == LetterResult.Correct));

        if (isSolved)
        {
            attempt.Status = AttemptStatus.Solved;
            attempt.CompletedOn = DateTime.UtcNow;
        }
        else if (guesses.Count >= _options.MaxGuesses)
        {
            attempt.Status = AttemptStatus.Failed;
            attempt.CompletedOn = DateTime.UtcNow;
        }
        else
        {
            attempt.Status = AttemptStatus.InProgress;
            attempt.CompletedOn = null;
        }
    }
}
