using FixFlow.Application.DTOs.Auth;
using FixFlow.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await auth.RegisterAsync(request, cancellationToken: cancellationToken));
    }

    [HttpPost("register/technician")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AuthResponse>> RegisterForm(
        [FromForm] RegisterRequest request,
        IFormFile? nicPhoto,
        IFormFile? certificate,
        IFormFile? profilePhoto,
        CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await auth.RegisterAsync(request, ToRegistrationFile(nicPhoto), ToRegistrationFile(certificate), ToRegistrationFile(profilePhoto), cancellationToken));
    }

    private static RegistrationFile? ToRegistrationFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        return new RegistrationFile
        {
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Content = file.OpenReadStream()
        };
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await auth.LoginAsync(request, cancellationToken));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.RefreshAsync(request, cancellationToken));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(request, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController(IAuthService auth, IValidator<UpdateProfileRequest> profileValidator, IValidator<ChangePasswordRequest> passwordValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await auth.GetMeAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<MeResponse>> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await profileValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await auth.UpdateProfileAsync(request, cancellationToken));
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await passwordValidator.ValidateAndThrowAsync(request, cancellationToken);
        await auth.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
