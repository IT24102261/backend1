using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Requests;

namespace FixFlow.Application.Interfaces;

public interface IRequestService
{
    Task<RequestDto> CreateAsync(RequestWriteRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<RequestDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<RequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RequestDto> UpdateAsync(Guid id, RequestWriteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RequestDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RequestDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MediaDto> AddMediaAsync(Guid id, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task AddClarificationAsync(Guid id, ClarificationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestHistoryDto>> HistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
