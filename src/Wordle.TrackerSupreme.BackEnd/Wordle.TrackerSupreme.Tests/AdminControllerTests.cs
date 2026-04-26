using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Api.Models.Admin;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Services;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class AdminControllerTests
{
    [Fact]
    public async Task GetPlayer_includes_attempt_stream()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Stream Admin",
            Email = "stream@example.com",
            PasswordHash = "hash",
            Attempts =
            [
                new PlayerPuzzleAttempt
                {
                    Id = Guid.NewGuid(),
                    PlayerId = Guid.NewGuid(),
                    DailyPuzzle = new DailyPuzzle
                    {
                        Id = Guid.NewGuid(),
                        PuzzleDate = new DateOnly(2025, 2, 1),
                        Stream = PuzzleStream.NewYorkTimes,
                        Solution = "CRANE"
                    },
                    Status = AttemptStatus.Solved,
                    PlayedInHardMode = true,
                    CreatedOn = DateTime.UtcNow,
                    CompletedOn = DateTime.UtcNow,
                    Guesses = []
                }
            ]
        };
        var controller = new AdminController(new FakeAdminService(player));

        var result = await controller.GetPlayer(player.Id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AdminPlayerDetailResponse>().Subject;
        response.Attempts.Should().ContainSingle();
        response.Attempts.Single().Stream.Should().Be(PuzzleStream.NewYorkTimes);
    }

    private sealed class FakeAdminService(Player player) : IAdminService
    {
        public Task<IReadOnlyList<Player>> GetPlayers(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Player>>([player]);
        }

        public Task<Player?> GetPlayer(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Player?>(player.Id == playerId ? player : null);
        }

        public Task<Player> UpdatePlayerProfile(
            Guid playerId,
            string displayName,
            string email,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Player> ResetPassword(Guid playerId, string newPassword, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Player> SetAdminStatus(Guid playerId, bool isAdmin, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<PlayerPuzzleAttempt> UpdateAttempt(
            Guid attemptId,
            IReadOnlyList<string> guesses,
            bool playedInHardMode,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAttempt(Guid attemptId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
