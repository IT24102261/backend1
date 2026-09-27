using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class AdminUserService(
    IRepository<User> users,
    IRepository<TechnicianProfile> technicians,
    IRepository<TechnicianCategoryApplication> applications,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher) : IAdminUserService
{
    public async Task<PagedResult<AdminUserDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = users.Query()
            .Include(x => x.TechnicianProfile)
                .ThenInclude(x => x!.Applications)
                .ThenInclude(x => x.Category)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = EnumMap.Parse<UserRole>(query.Role);
            source = source.Where(x => x.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (query.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
            {
                source = source.Where(x => x.Role == UserRole.Technician && !x.IsActive);
            }
            else
            {
                var active = query.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
                var inactive = query.Status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase);
                if (active || inactive)
                {
                    source = source.Where(x => x.IsActive == active);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(x => x.Email.Contains(term) || x.DisplayName.Contains(term));
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source
            .OrderByDescending(x => x.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminUserDto>
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.Take,
            TotalCount = total
        };
    }

    public async Task<AdminUserDto> CreateAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var role = EnumMap.Parse<UserRole>(request.Role);
        if (role == UserRole.Admin)
        {
            throw new ForbiddenException("Admin accounts cannot be created from this screen.");
        }

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
            Role = role
        };
        await users.AddAsync(user, cancellationToken);

        if (role == UserRole.Technician)
        {
            var profile = new TechnicianProfile
            {
                UserId = user.Id,
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                ServiceArea = string.IsNullOrWhiteSpace(request.ServiceArea) ? null : request.ServiceArea.Trim()
            };
            await technicians.AddAsync(profile, cancellationToken);
            if (request.CategoryId.HasValue && request.CategoryId != Guid.Empty)
            {
                await applications.AddAsync(new TechnicianCategoryApplication
                {
                    TechnicianId = profile.Id,
                    CategoryId = request.CategoryId.Value,
                    Status = ApplicationStatus.Submitted
                }, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var created = await users.Query().Include(x => x.TechnicianProfile).FirstAsync(x => x.Id == user.Id, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        return Map(created);
    }

    public async Task<AdminUserDto> SetActiveAsync(Guid id, AdminUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.Query().Include(x => x.TechnicianProfile).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        if (user.Role == UserRole.Admin && !request.IsActive)
        {
            throw new ForbiddenException("Admin accounts cannot be deactivated here.");
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private static AdminUserDto Map(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Phone = user.Phone,
        Role = EnumMap.ToApi(user.Role),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        TechnicianProfileId = user.TechnicianProfile?.Id,
        Address = user.TechnicianProfile?.Address,
        ServiceArea = user.TechnicianProfile?.ServiceArea,
        RequestedCategory = user.TechnicianProfile?.Applications
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => x.Category.Name)
            .FirstOrDefault(),
        LoginStatus = user.Role == UserRole.Technician && !user.IsActive
            ? "PENDING"
            : user.IsActive ? "ACTIVE" : "INACTIVE"
    };
}
