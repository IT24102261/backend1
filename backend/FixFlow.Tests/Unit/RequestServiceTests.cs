using FixFlow.Application.DTOs.Maps;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Services;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FixFlow.Tests.Unit;

public class RequestServiceTests
{
    [Fact]
    public async Task Create_DoesNotCallMapService_WhenCoordinatesArePresent()
    {
        var maps = new Mock<IMapService>(MockBehavior.Strict);
        var orchestrator = new Mock<IAgentOrchestrator>(MockBehavior.Loose);
        var files = new Mock<IFileStorage>(MockBehavior.Strict);
        var user = new TestCurrentUser { Role = UserRole.Customer, UserId = Guid.NewGuid() };
        var requests = new InMemoryRepository<ServiceRequest>();
        var service = new RequestService(
            requests,
            new InMemoryRepository<RequestMedia>(),
            new InMemoryRepository<RequestClarification>(),
            new InMemoryRepository<RequestStatusHistory>(),
            new InMemoryRepository<TechnicianProfile>(),
            new InMemoryRepository<RequestInvitation>(),
            new InMemoryRepository<Quotation>(),
            new InMemoryRepository<Booking>(),
            new InMemoryRepository<BookingStatusHistory>(),
            orchestrator.Object,
            maps.Object,
            files.Object,
            new Mock<INotificationService>().Object,
            new InMemoryUnitOfWork(),
            user,
            NullLogger<RequestService>.Instance);

        var created = await service.CreateAsync(new RequestWriteRequest
        {
            Description = "Two wall switches are not working.",
            ServiceArea = "Colombo",
            Address = "12 Flower Road",
            Latitude = 6.9,
            Longitude = 79.8
        });

        Assert.Equal(user.UserId, created.CustomerId);
        Assert.Equal("DRAFT", created.Status);
        Assert.Single(requests.Items);
        maps.Verify(x => x.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_GeocodesWhenCoordinatesMissing()
    {
        var maps = new Mock<IMapService>();
        maps.Setup(x => x.GeocodeAddressAsync("12 Flower Road", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodeResult { Found = true, Latitude = 6.91, Longitude = 79.86 });
        var user = new TestCurrentUser { Role = UserRole.Customer, UserId = Guid.NewGuid() };
        var service = new RequestService(
            new InMemoryRepository<ServiceRequest>(),
            new InMemoryRepository<RequestMedia>(),
            new InMemoryRepository<RequestClarification>(),
            new InMemoryRepository<RequestStatusHistory>(),
            new InMemoryRepository<TechnicianProfile>(),
            new InMemoryRepository<RequestInvitation>(),
            new InMemoryRepository<Quotation>(),
            new InMemoryRepository<Booking>(),
            new InMemoryRepository<BookingStatusHistory>(),
            new Mock<IAgentOrchestrator>().Object,
            maps.Object,
            new Mock<IFileStorage>().Object,
            new Mock<INotificationService>().Object,
            new InMemoryUnitOfWork(),
            user,
            NullLogger<RequestService>.Instance);

        var created = await service.CreateAsync(new RequestWriteRequest
        {
            Description = "Need an electrician for a dead switch.",
            Address = "12 Flower Road"
        });

        Assert.Equal(6.91, created.Latitude);
        Assert.Equal(79.86, created.Longitude);
        maps.Verify(x => x.GeocodeAddressAsync("12 Flower Road", It.IsAny<CancellationToken>()), Times.Once);
    }
}
