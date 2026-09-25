using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;

namespace FixFlow.Application.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<AdminUserDto> CreateAsync(AdminCreateUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDto> SetActiveAsync(Guid id, AdminUserStatusRequest request, CancellationToken cancellationToken = default);
}
