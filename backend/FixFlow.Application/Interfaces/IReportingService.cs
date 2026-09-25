using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Reporting;

namespace FixFlow.Application.Interfaces;

public interface IReportingService
{
    Task<DashboardDto> DashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<RequestReportDto>> RequestsAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<BookingReportDto>> BookingsAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<TechnicianReportDto>> TechniciansAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<AgentReportDto>> AgentsAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogDto>> AuditAsync(PagedQuery query, CancellationToken cancellationToken = default);
}
