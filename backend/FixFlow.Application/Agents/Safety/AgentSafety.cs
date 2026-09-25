using System.Text.Json;
using System.Text.RegularExpressions;
using FixFlow.Application.Agents.Contracts;

namespace FixFlow.Application.Agents.Safety;

public static class AgentSafety
{
    public const int DefaultMaxChars = 4000;
    private static readonly Regex Injection = new(
        @"ignore (all|any|previous|prior)|system\s*:|you are now|act as (an? )?(admin|root)|call tool|execute tool|authorization:\s*bearer|api[_-]?key|password\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string SanitizeUserText(string? value, int maxChars = DefaultMaxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxChars ? trimmed : trimmed[..maxChars];
    }

    public static bool LooksLikeInjection(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Injection.IsMatch(value);

    public static bool IsPrimarilyInjection(string? value)
    {
        if (!LooksLikeInjection(value))
        {
            return false;
        }

        var text = value ?? string.Empty;
        var hasJobLanguage = Regex.IsMatch(
            text,
            @"switch|outlet|socket|plumb|leak|ac\b|air.?con|paint|carpenter|solar|repair|replace|install|not working",
            RegexOptions.IgnoreCase);
        return !hasJobLanguage;
    }

    public static bool IsDangerousElectrical(string? value) =>
        Regex.IsMatch(
            value ?? string.Empty,
            @"live wire|mains rewire|fuse box|high voltage|climb (the )?pole|diy electrical|shock|rewire the (house|mains)",
            RegexOptions.IgnoreCase);

    public static JsonDocument ParseJsonObject(string? raw, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new AgentOutputException("MALFORMED_AI_RESPONSE", "The model returned an empty payload.");
        }

        var text = raw.Trim();
        if (text.Length > maxChars)
        {
            text = text[..maxChars];
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new AgentOutputException("MALFORMED_AI_RESPONSE", "The model did not return a JSON object.");
        }

        try
        {
            return JsonDocument.Parse(text[start..(end + 1)]);
        }
        catch (JsonException ex)
        {
            throw new AgentOutputException("MALFORMED_AI_RESPONSE", "The model returned invalid JSON.", ex);
        }
    }

    public static T Deserialize<T>(string? raw, int maxChars)
    {
        using var document = ParseJsonObject(raw, maxChars);
        return document.RootElement.Deserialize<T>(AgentJson.Options)
            ?? throw new AgentOutputException("MALFORMED_AI_RESPONSE", "The model JSON could not be mapped.");
    }
}

public class AgentOutputException : Exception
{
    public AgentOutputException(string code, string message, Exception? inner = null) : base(message, inner)
    {
        Code = code;
    }

    public string Code { get; }
}
