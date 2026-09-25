using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Complaints;

namespace FixFlow.Application.Interfaces;

public interface IComplaintService
{
    Task<ComplaintDto> CreateAsync(ComplaintWriteRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ComplaintDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<ComplaintDto>> AdminListAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<ComplaintDto> UpdateStatusAsync(Guid id, ComplaintStatusRequest request, CancellationToken cancellationToken = default);
    Task<ComplaintDto> ReplyAsync(Guid id, AdminReplyRequest request, CancellationToken cancellationToken = default);
}
