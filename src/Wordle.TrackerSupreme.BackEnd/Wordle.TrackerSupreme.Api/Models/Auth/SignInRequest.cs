using System.ComponentModel.DataAnnotations;
using Wordle.TrackerSupreme.Domain.Validation;

namespace Wordle.TrackerSupreme.Api.Models.Auth;

public class SignInRequest
{
    [Required]
    [EmailAddress]
    [StringLength(PlayerValidationRules.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(PlayerValidationRules.PasswordMaxLength, MinimumLength = PlayerValidationRules.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;
}
