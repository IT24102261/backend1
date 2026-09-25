using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Rules;

public static class BookingStateMachine
{
    private static readonly Dictionary<BookingStatus, BookingStatus[]> Allowed = new()
    {
        [BookingStatus.PendingValidation] = [BookingStatus.Confirmed, BookingStatus.Cancelled],
        [BookingStatus.Confirmed] = [BookingStatus.Accepted, BookingStatus.Cancelled, BookingStatus.Disputed],
        [BookingStatus.Accepted] = [BookingStatus.EnRoute, BookingStatus.Cancelled, BookingStatus.Disputed],
        [BookingStatus.EnRoute] = [BookingStatus.InProgress, BookingStatus.Cancelled, BookingStatus.Disputed],
        [BookingStatus.InProgress] = [BookingStatus.WorkCompleted, BookingStatus.Disputed],
        [BookingStatus.WorkCompleted] = [BookingStatus.CustomerConfirmed, BookingStatus.Disputed],
        [BookingStatus.CustomerConfirmed] = [BookingStatus.Closed, BookingStatus.Disputed],
        [BookingStatus.Disputed] = [BookingStatus.Closed, BookingStatus.Cancelled],
        [BookingStatus.Closed] = [],
        [BookingStatus.Cancelled] = []
    };

    public static readonly BookingStatus[] Active =
    [
        BookingStatus.PendingValidation,
        BookingStatus.Confirmed,
        BookingStatus.Accepted,
        BookingStatus.EnRoute,
        BookingStatus.InProgress,
        BookingStatus.WorkCompleted,
        BookingStatus.CustomerConfirmed,
        BookingStatus.Disputed
    ];

    public static readonly BookingStatus[] TechnicianOperational =
    [
        BookingStatus.Accepted,
        BookingStatus.EnRoute,
        BookingStatus.InProgress,
        BookingStatus.WorkCompleted
    ];

    public static bool CanTransition(BookingStatus from, BookingStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static bool IsActive(BookingStatus status) => Active.Contains(status);
}
