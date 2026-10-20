using System.Text.RegularExpressions;

namespace Application.Features.CustomerManager.Services;

/// <summary>
/// Bangladeshi mobile numbers — the only format the customer validators accept
/// (<c>^(?:\+?88)?01[3-9]\d{8}$</c>). The canonical form is the 11-digit local number
/// (<c>01XXXXXXXXX</c>): <c>01711234567</c>, <c>8801711234567</c> and <c>+8801711234567</c> are the
/// same subscriber, so they normalize to the same value. Nothing else is folded together.
/// </summary>
public static partial class CustomerMobileNumber
{
    public const string InvalidMessage = "Enter a valid mobile number (e.g. 01711234567 or +8801711234567).";

    [GeneratedRegex(@"^(?:\+?88)?(01[3-9]\d{8})$")]
    private static partial Regex MobilePattern();

    [GeneratedRegex(@"[\s\-().]")]
    private static partial Regex Separators();

    /// <summary>
    /// Strips the separators people type or paste (spaces, dashes, dots, parentheses) and returns
    /// the canonical local number, or false when the input is not a supported mobile number.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var match = MobilePattern().Match(Separators().Replace(input.Trim(), string.Empty));
        if (!match.Success)
            return false;

        normalized = match.Groups[1].Value;
        return true;
    }

    public static bool IsValid(string? input) => TryNormalize(input, out _);
}
