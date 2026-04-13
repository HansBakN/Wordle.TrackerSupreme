using System.ComponentModel.DataAnnotations;

namespace Wordle.TrackerSupreme.Domain.Validation;

public static class PlayerValidationRules
{
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 200;
    public const int EmailMaxLength = 320;
    public const int PasswordMinLength = 6;
    public const int PasswordMaxLength = 100;

    public static void EnsureValidDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (displayName.Length < DisplayNameMinLength || displayName.Length > DisplayNameMaxLength)
        {
            throw new ArgumentException(
                $"Display name must be between {DisplayNameMinLength} and {DisplayNameMaxLength} characters.",
                nameof(displayName));
        }
    }

    public static void EnsureValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (email.Length > EmailMaxLength)
        {
            throw new ArgumentException($"Email cannot exceed {EmailMaxLength} characters.", nameof(email));
        }

        var validator = new EmailAddressAttribute();
        if (!validator.IsValid(email))
        {
            throw new ArgumentException("Email must be a valid email address.", nameof(email));
        }
    }

    public static void EnsureValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
        {
            throw new ArgumentException(
                $"Password must be between {PasswordMinLength} and {PasswordMaxLength} characters.",
                nameof(password));
        }
    }
}
