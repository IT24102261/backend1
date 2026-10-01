using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Auth;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Application.Services;

public class AuthService(
    IRepository<User> users,
    IRepository<RefreshToken> refreshTokens,
    IRepository<TechnicianProfile> technicians,
    IRepository<TechnicianProfileImage> profileImages,
    IRepository<TechnicianCategoryApplication> applications,
    IRepository<TechnicianDocument> documents,
    IRepository<ServiceCategory> categories,
    IFileStorage files,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokens,
    ICurrentUser currentUser,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        RegistrationFile? nicPhoto = null,
        RegistrationFile? certificate = null,
        RegistrationFile? profilePhoto = null,
        CancellationToken cancellationToken = default)
    {
        var role = ParseRegisterRole(request.Role);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.Query().AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with that email already exists.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            DisplayName = request.DisplayName.Trim(),
            Phone = InputRules.NormalizePhone(request.Phone),
            Role = role,
            IsActive = role != UserRole.Technician
        };

        await users.AddAsync(user, cancellationToken);
        if (role == UserRole.Technician)
        {
            var category = await categories.GetByIdAsync(request.CategoryId ?? Guid.Empty, cancellationToken)
                ?? throw new NotFoundException("Select a trade such as plumber or painter.");
            if (!category.IsActive)
            {
                throw new ConflictException("That service field is not available.");
            }

            if (profilePhoto is null)
            {
                throw new ValidationFailedException("Upload a profile photo.");
            }

            if (nicPhoto is null)
            {
                throw new ValidationFailedException("Upload a photo of your NIC.");
            }

            var requiresCertificate = IsElectrician(category);
            if (requiresCertificate && certificate is null)
            {
                throw new ValidationFailedException("Electricians must upload their studied certificate.");
            }

            var profile = new TechnicianProfile
            {
                UserId = user.Id,
                Address = request.Address?.Trim()
            };
            await technicians.AddAsync(profile, cancellationToken);
            var (photoKey, photoBytes) = await StoreImageAsync($"profiles/{profile.Id}", profilePhoto, cancellationToken);
            profile.ProfilePhotoStorageKey = photoKey;
            profile.ProfilePhotoMimeType = profilePhoto.ContentType;
            await profileImages.AddAsync(new TechnicianProfileImage
            {
                TechnicianId = profile.Id,
                Content = photoBytes,
                MimeType = profilePhoto.ContentType
            }, cancellationToken);
            var application = new TechnicianCategoryApplication
            {
                TechnicianId = profile.Id,
                CategoryId = category.Id,
                Status = ApplicationStatus.Submitted
            };
            await applications.AddAsync(application, cancellationToken);
            await StoreDocumentAsync(application.Id, EvidenceType.Identity, nicPhoto, imageOnly: true, cancellationToken);
            if (certificate is not null)
            {
                await StoreDocumentAsync(application.Id, EvidenceType.Certificate, certificate, imageOnly: false, cancellationToken);
            }
        }

        if (role == UserRole.Technician)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Technician {UserId} registered and is waiting for admin login approval", user.Id);
            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Role = EnumMap.ToApi(role),
                RequiresAdminApproval = true
            };
        }

        var response = await IssueAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} registered as {Role}", user.Id, EnumMap.ToApi(role));
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.Query().FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new UnauthorizedAppException();

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException();
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAppException(user.Role == UserRole.Technician
                ? "Your technician account is waiting for admin approval."
                : "This account is not active.");
        }

        var response = await IssueAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} signed in", user.Id);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await refreshTokens.Query()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedAppException("Refresh token is invalid.");

        if (!stored.IsActive || !stored.User.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token is expired or revoked.");
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        var response = await IssueAsync(stored.User, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await refreshTokens.FirstAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is not null && stored.IsActive)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var user = await users.Query()
            .Include(x => x.TechnicianProfile)
            .FirstOrDefaultAsync(x => x.Id == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var approved = Array.Empty<string>();
        if (user.TechnicianProfile is not null)
        {
            approved = await applications.Query()
                .Include(x => x.Category)
                .Where(x => x.TechnicianId == user.TechnicianProfile.Id && x.Status == ApplicationStatus.Approved)
                .Select(x => x.Category.Name)
                .ToArrayAsync(cancellationToken);
        }

        return new MeResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            Role = EnumMap.ToApi(user.Role),
            IsActive = user.IsActive,
            TechnicianProfileId = user.TechnicianProfile?.Id,
            ApprovedCategories = approved
        };
    }

    public async Task<MeResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        user.DisplayName = request.DisplayName.Trim();
        user.Phone = InputRules.NormalizePhone(request.Phone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetMeAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Current password is incorrect.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        var tokensForUser = await refreshTokens.Query().Where(x => x.UserId == user.Id && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokensForUser)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} changed password", user.Id);
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var access = tokens.CreateAccessToken(user.Id, user.Email, user.Role, out var expires);
        var refresh = tokens.CreateRefreshToken();
        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        }, cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAtUtc = expires,
            Email = user.Email,
            Role = EnumMap.ToApi(user.Role)
        };
    }

    private async Task<(string Key, byte[] Bytes)> StoreImageAsync(string folder, RegistrationFile file, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await file.Content.CopyToAsync(buffer, cancellationToken);
        UploadRules.EnsureImage(file.ContentType, buffer.Length);
        var bytes = buffer.ToArray();
        var key = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        try
        {
            buffer.Position = 0;
            key = await files.SaveAsync(folder, file.FileName, buffer, cancellationToken);
        }
        catch (IOException)
        {
            // The database copy is the copy that survives a deploy.
        }

        return (key, bytes);
    }

    private async Task StoreDocumentAsync(
        Guid applicationId,
        EvidenceType evidenceType,
        RegistrationFile file,
        bool imageOnly,
        CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await file.Content.CopyToAsync(buffer, cancellationToken);
        if (imageOnly)
        {
            UploadRules.EnsureImage(file.ContentType, buffer.Length);
        }
        else
        {
            UploadRules.EnsureDocument(file.ContentType, buffer.Length);
        }

        var sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray()));
        buffer.Position = 0;
        var key = await files.SaveAsync($"applications/{applicationId}", file.FileName, buffer, cancellationToken);
        await documents.AddAsync(new TechnicianDocument
        {
            ApplicationId = applicationId,
            EvidenceType = evidenceType,
            StorageKey = key,
            MimeType = file.ContentType,
            Sha256 = sha256
        }, cancellationToken);
    }

    private static bool IsElectrician(ServiceCategory category) =>
        category.Id == ServiceCategorySeed.Electrician
        || category.Name.Equals("Electrician", StringComparison.OrdinalIgnoreCase);

    private static UserRole ParseRegisterRole(string role)
    {
        var parsed = EnumMap.Parse<UserRole>(role);
        if (parsed == UserRole.Admin)
        {
            throw new ForbiddenException("Admin accounts cannot be self-registered.");
        }

        return parsed;
    }
}
