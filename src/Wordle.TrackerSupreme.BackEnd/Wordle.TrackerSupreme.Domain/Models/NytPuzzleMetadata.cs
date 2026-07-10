namespace Wordle.TrackerSupreme.Domain.Models;

public sealed record NytPuzzleMetadata(int NytPuzzleId, DateOnly PrintDate, int? PublicPuzzleNumber, string? Solution = null);
