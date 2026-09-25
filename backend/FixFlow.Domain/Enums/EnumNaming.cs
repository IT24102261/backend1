using System.Text;

namespace FixFlow.Domain.Enums;

public static class EnumNaming
{
    public static string ToUpperSnake(string pascalCase)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var character = pascalCase[i];
            if (char.IsUpper(character) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }

    public static TEnum FromUpperSnake<TEnum>(string value) where TEnum : struct, Enum
    {
        foreach (var member in Enum.GetValues<TEnum>())
        {
            if (ToUpperSnake(member.ToString()) == value)
            {
                return member;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {typeof(TEnum).Name} value.");
    }
}
