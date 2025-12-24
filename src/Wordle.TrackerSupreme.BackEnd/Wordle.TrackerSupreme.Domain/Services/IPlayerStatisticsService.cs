using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services;

public interface IPlayerStatisticsService
{
    PlayerStatistics Calculate(
        Player player,
        PlayerStatisticsFilter? filter = null,
        Func<PlayerPuzzleAttempt, bool>? isAfterReveal = null);
}
