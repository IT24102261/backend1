using System.Text.RegularExpressions;

namespace FixFlow.Application.Common;

public static class InputRules
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var email = value.Trim();
        return email.Count(c => c == '@') == 1 && EmailPattern.IsMatch(email);
    }

    public static string Digits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

    public static bool IsTenDigitPhone(string? value) => Digits(value).Length == 10;

    public static string? NormalizePhone(string? value)
    {
        var digits = Digits(value);
        return digits.Length == 0 ? null : digits;
    }

    public static bool IsFuture(DateTimeOffset? value) =>
        value is DateTimeOffset instant && instant > DateTimeOffset.UtcNow;
}
