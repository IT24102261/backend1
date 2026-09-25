using FixFlow.Application.DTOs.Notifications;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class NotificationService(
    IRepository<Notification> notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : INotificationService
{
    public async Task NotifyAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        await notifications.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var items = await notifications.Query()
            .Where(x => x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        return items.Select(x => new NotificationDto
        {
            Id = x.Id,
            Title = x.Title,
            Message = x.Message,
            IsRead = x.IsRead,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await notifications.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
        if (item.UserId != currentUser.UserId && !currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        item.IsRead = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
