namespace FixFlow.Application.Common;

public static class TechnicianPhotoUrl
{
    public static string? For(Guid technicianId, string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var fileName = Path.GetFileName(storageKey.Replace('\\', '/'));
        var version = fileName.Split('_')[0];
        if (version.Length > 32)
        {
            version = version[..32];
        }

        return $"/api/technicians/{technicianId}/photo?v={version}";
    }
}
