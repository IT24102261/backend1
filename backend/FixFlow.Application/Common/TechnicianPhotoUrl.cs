namespace FixFlow.Application.Common;

public static class TechnicianPhotoUrl
{
    public static string? For(Guid technicianId, string? storageKey) =>
        string.IsNullOrWhiteSpace(storageKey) ? null : $"/api/technicians/{technicianId}/photo";
}
