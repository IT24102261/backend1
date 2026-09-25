using FixFlow.Domain.Enums;

namespace FixFlow.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(Guid userId, string email, UserRole role, out DateTime expiresAtUtc);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
