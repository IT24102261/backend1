using FixFlow.Application.DTOs.Notifications;

namespace FixFlow.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default);
}
