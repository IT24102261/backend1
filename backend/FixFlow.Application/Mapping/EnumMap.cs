using FixFlow.Domain.Enums;

namespace FixFlow.Application.Mapping;

public static class EnumMap
{
    public static string ToApi<TEnum>(TEnum value) where TEnum : struct, Enum =>
        EnumNaming.ToUpperSnake(value.ToString());

    public static TEnum Parse<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Enum value is required.");
        }

        return EnumNaming.FromUpperSnake<TEnum>(value.Trim());
    }

    public static TEnum? TryParse<TEnum>(string? value) where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? null : EnumNaming.FromUpperSnake<TEnum>(value.Trim());
}
