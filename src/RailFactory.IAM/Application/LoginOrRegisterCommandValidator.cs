using System.Text.RegularExpressions;

namespace RailFactory.IAM.Application;

/// <summary>
/// Validates OAuth callback input (RNF-10). Rejects invalid data before creating or updating user.
/// </summary>
public static class LoginOrRegisterCommandValidator
{
    public const int MaxEmailLength = 256;
    public const int MaxExternalIdLength = 256;
    public const int MaxNameLength = 256;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Returns validation errors; empty list if valid.
    /// </summary>
    public static IReadOnlyList<string> Validate(LoginOrRegisterCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.ExternalId))
            errors.Add("ExternalId is required.");
        else if (command.ExternalId.Length > MaxExternalIdLength)
            errors.Add($"ExternalId must be at most {MaxExternalIdLength} characters.");

        if (string.IsNullOrWhiteSpace(command.Email))
            errors.Add("Email is required.");
        else
        {
            var email = command.Email.Trim();
            if (email.Length > MaxEmailLength)
                errors.Add($"Email must be at most {MaxEmailLength} characters.");
            else if (!EmailRegex.IsMatch(email))
                errors.Add("Email format is invalid.");
        }

        if (command.Name is { Length: > MaxNameLength })
            errors.Add($"Name must be at most {MaxNameLength} characters.");

        return errors;
    }
}
