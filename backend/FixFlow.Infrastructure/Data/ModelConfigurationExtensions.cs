using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FixFlow.Infrastructure.Data;

internal static class ModelConfigurationExtensions
{
    public static ValueConverter<TEnum, string> UpperSnakeConverter<TEnum>()
        where TEnum : struct, Enum =>
        new(
            value => EnumNaming.ToUpperSnake(value.ToString()),
            value => EnumNaming.FromUpperSnake<TEnum>(value));

    public static PropertyBuilder<TEnum> HasUpperSnakeConversion<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum =>
        property.HasConversion(UpperSnakeConverter<TEnum>()).HasMaxLength(64);

    public static void UseXminConcurrency(this EntityTypeBuilder builder)
    {
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
