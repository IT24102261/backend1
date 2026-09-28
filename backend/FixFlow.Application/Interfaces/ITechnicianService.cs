using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Technicians;

namespace FixFlow.Application.Interfaces;

public interface ITechnicianService
{
    Task<TechnicianProfileDto> CreateProfileAsync(TechnicianProfileRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<TechnicianProfileDto> UpdateProfileAsync(TechnicianProfileRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> ApplyAsync(TechnicianApplicationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnicianApplicationDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> GetMineAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentDto> AddDocumentAsync(Guid applicationId, string evidenceType, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task<TechnicianPhotoFile> GetDocumentFileAsync(Guid applicationId, Guid documentId, CancellationToken cancellationToken = default);
    Task<PagedResult<TechnicianApplicationDto>> AdminListAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> AdminGetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> ApproveAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> RejectAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> RequestInfoAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> RequestReverificationAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianApplicationDto> SuspendApplicationAsync(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<PublicTechnicianDto> GetPublicAsync(Guid technicianId, CancellationToken cancellationToken = default);
    Task<TechnicianPhotoFile?> GetPhotoAsync(Guid technicianId, CancellationToken cancellationToken = default);
    Task<TechnicianProfileDto> SetProfilePhotoAsync(Guid technicianId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task<TechnicianProfileDto> SuspendTechnicianAsync(Guid technicianId, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<TechnicianProfileDto> ReactivateTechnicianAsync(Guid technicianId, ApplicationDecisionRequest request, CancellationToken cancellationToken = default);
}
