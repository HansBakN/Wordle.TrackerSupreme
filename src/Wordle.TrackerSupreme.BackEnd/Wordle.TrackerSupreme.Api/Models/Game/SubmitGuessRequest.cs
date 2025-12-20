using System.ComponentModel.DataAnnotations;

namespace Wordle.TrackerSupreme.Api.Models.Game;

public class SubmitGuessRequest
{
    [Required]
    [StringLength(5, MinimumLength = 5)]
    public string Guess { get; set; } = string.Empty;
}
