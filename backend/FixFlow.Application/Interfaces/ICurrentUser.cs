using FixFlow.Domain.Enums;

namespace FixFlow.Application.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsCustomer { get; }
    bool IsTechnician { get; }
}
