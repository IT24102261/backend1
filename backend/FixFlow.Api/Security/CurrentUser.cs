using System.Security.Claims;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Enums;

namespace FixFlow.Api.Security;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId => Guid.TryParse(Find("userId") ?? Find(ClaimTypes.NameIdentifier) ?? Find("sub"), out var id) ? id : Guid.Empty;
    public string Email => Find("email") ?? Find(ClaimTypes.Email) ?? string.Empty;
    public UserRole Role => EnumMap.Parse<UserRole>(Find("role") ?? Find(ClaimTypes.Role) ?? "CUSTOMER");
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsCustomer => Role == UserRole.Customer;
    public bool IsTechnician => Role == UserRole.Technician;

    private string? Find(string type) => accessor.HttpContext?.User.FindFirstValue(type);
}
