using FixFlow.Application.DTOs.Auth;

namespace FixFlow.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        RegistrationFile? nicPhoto = null,
        RegistrationFile? certificate = null,
        RegistrationFile? profilePhoto = null,
        CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<MeResponse> GetMeAsync(CancellationToken cancellationToken = default);
    Task<MeResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
