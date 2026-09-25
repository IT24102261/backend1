using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Reviews;

namespace FixFlow.Application.Interfaces;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(Guid bookingId, ReviewWriteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> ListForTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewDto>> AdminListAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<ReviewDto> ModerateAsync(Guid id, ModerateReviewRequest request, CancellationToken cancellationToken = default);
    Task<ReviewDto> UpdateAsync(Guid id, AdminReviewUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ReviewDto> ReplyAsync(Guid id, AdminReplyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
