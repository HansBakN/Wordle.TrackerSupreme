using System.ComponentModel.DataAnnotations;

namespace Wordle.TrackerSupreme.Api.Models.Admin;

public class AdminCreatePuzzleRequest
{
    [Required]
    public DateOnly PuzzleDate { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 1)]
    public string Solution { get; set; } = string.Empty;
}
