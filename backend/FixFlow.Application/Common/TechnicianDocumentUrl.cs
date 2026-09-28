namespace FixFlow.Application.Common;

public static class TechnicianDocumentUrl
{
    public static string For(Guid applicationId, Guid documentId) =>
        $"/api/technician-applications/{applicationId}/documents/{documentId}";
}
