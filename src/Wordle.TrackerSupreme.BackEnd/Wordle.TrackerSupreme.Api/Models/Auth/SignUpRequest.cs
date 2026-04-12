using System.ComponentModel.DataAnnotations;
using Wordle.TrackerSupreme.Domain.Validation;

namespace Wordle.TrackerSupreme.Api.Models.Auth;

public class SignUpRequest
{
    [Required]
    [StringLength(PlayerValidationRules.DisplayNameMaxLength, MinimumLength = PlayerValidationRules.DisplayNameMinLength)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(PlayerValidationRules.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(PlayerValidationRules.PasswordMaxLength, MinimumLength = PlayerValidationRules.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;
}
