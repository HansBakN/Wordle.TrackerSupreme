using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Domain.Services;

public interface IPlayerStatisticsService
{
    PlayerStatistics Calculate(Player player, Func<PlayerPuzzleAttempt, bool>? isAfterReveal = null);
}
