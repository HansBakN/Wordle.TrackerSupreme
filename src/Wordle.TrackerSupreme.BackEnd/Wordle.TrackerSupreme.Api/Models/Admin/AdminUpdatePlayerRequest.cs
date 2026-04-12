using System.ComponentModel.DataAnnotations;
using Wordle.TrackerSupreme.Domain.Validation;

namespace Wordle.TrackerSupreme.Api.Models.Admin;

public record AdminUpdatePlayerRequest(
    [Required]
    [StringLength(PlayerValidationRules.DisplayNameMaxLength, MinimumLength = PlayerValidationRules.DisplayNameMinLength)]
    string DisplayName,
    [Required]
    [EmailAddress]
    [StringLength(PlayerValidationRules.EmailMaxLength)]
    string Email);
