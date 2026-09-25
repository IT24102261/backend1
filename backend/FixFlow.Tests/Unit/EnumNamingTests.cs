using FixFlow.Domain.Enums;

namespace FixFlow.Tests.Unit;

public class EnumNamingTests
{
    [Fact]
    public void ToUpperSnake_ConvertsPascalCase()
    {
        Assert.Equal("MORE_INFORMATION_REQUIRED", EnumNaming.ToUpperSnake(nameof(ApplicationStatus.MoreInformationRequired)));
        Assert.Equal("CUSTOMER", EnumNaming.ToUpperSnake(nameof(UserRole.Customer)));
    }

    [Fact]
    public void FromUpperSnake_ParsesStoredValues()
    {
        Assert.Equal(ApplicationStatus.MoreInformationRequired, EnumNaming.FromUpperSnake<ApplicationStatus>("MORE_INFORMATION_REQUIRED"));
        Assert.Equal(UserRole.Admin, EnumNaming.FromUpperSnake<UserRole>("ADMIN"));
    }
}
