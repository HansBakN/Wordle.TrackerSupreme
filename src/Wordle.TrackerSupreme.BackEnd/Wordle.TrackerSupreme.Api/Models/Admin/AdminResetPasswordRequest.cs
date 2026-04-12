using System.ComponentModel.DataAnnotations;
using Wordle.TrackerSupreme.Domain.Validation;

namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminResetPasswordRequest(
    [Required]
    [StringLength(PlayerValidationRules.PasswordMaxLength, MinimumLength = PlayerValidationRules.PasswordMinLength)]
    string Password);
