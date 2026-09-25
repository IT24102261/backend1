using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Data;
using FixFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class BookingApiTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task SelectQuote_AnotherCustomer_IsForbidden()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var stranger = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(stranger.AccessToken);

        var response = await client.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SelectQuote_Expired_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        await scenario.ExpireQuoteAsync(fixture, scenario.QuoteId!.Value);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var response = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SelectQuote_WithoutCategoryApproval_IsRejected()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        Guid quoteId = Guid.Empty;
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var profile = await db.TechnicianProfiles.SingleAsync(x => x.UserId == technician.UserId);
            var request = new ServiceRequest
            {
                CustomerId = customer.UserId,
                CategoryId = ServiceCategorySeed.Plumber,
                Description = "Burst pipe under the sink.",
                ServiceArea = "Colombo",
                Status = ServiceRequestStatus.CollectingQuotes
            };
            db.ServiceRequests.Add(request);
            var quote = new Quotation
            {
                RequestId = request.Id,
                TechnicianId = profile.Id,
                LabourAmount = 2000,
                MaterialsAmount = 0,
                TravelAmount = 0,
                TotalAmount = 2000,
                DurationMinutes = 45,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                Status = QuotationStatus.Sent
            };
            db.Quotations.Add(quote);
            await db.SaveChangesAsync();
            quoteId = quote.Id;
        });

        using var client = fixture.CreateClient(customer.AccessToken);
        var response = await client.PostAsync($"/api/quotes/{quoteId}/select", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SelectQuote_ConflictingArrival_IsRejected()
    {
        var start = DateTimeOffset.UtcNow.AddDays(3).AddHours(10);
        var first = await MarketplaceScenario.CreateAsync(fixture, arrivalStart: start);
        var firstBooking = await first.SelectQuoteAsync(fixture);
        await first.ConfirmAsync(fixture, firstBooking.Id);

        var second = await MarketplaceScenario.CreateAsync(fixture, arrivalStart: start.AddMinutes(15));
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var quote = await db.Quotations.SingleAsync(x => x.Id == second.QuoteId);
            quote.TechnicianId = first.TechnicianProfileId;
            await db.SaveChangesAsync();
        });

        using var customer = fixture.CreateClient(second.Customer.AccessToken);
        var response = await customer.PostAsync($"/api/quotes/{second.QuoteId}/select", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SelectQuote_MissingApproval_IsRejectedByAgentFour()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var result = await fixture.ScopeAsync(async services =>
        {
            var orchestrator = services.GetRequiredService<IAgentOrchestrator>();
            return await orchestrator.ValidateSelectedQuoteAsync(
                scenario.RequestId,
                scenario.QuoteId!.Value,
                scenario.Customer.UserId);
        });

        Assert.False(result.Validation?.BookingAllowed);
        Assert.Contains(result.Validation?.Errors ?? [], x => x.Contains("approval", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidBooking_UsesTransactionAndReleasesAddressOnConfirm()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var select = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);
        Assert.Equal(HttpStatusCode.OK, select.StatusCode);
        var booking = (await select.Content.ReadFromJsonAsync<BookingDto>(FixFlowApiFixture.Json))!;
        Assert.Equal("PENDING_VALIDATION", booking.Status);
        Assert.Null(booking.ConfirmedAt);

        var confirmed = await scenario.ConfirmAsync(fixture, booking.Id);
        Assert.Equal("CONFIRMED", confirmed.Status);
        Assert.NotNull(confirmed.ConfirmedAt);
        Assert.NotNull(confirmed.AddressReleaseAt);
        Assert.Equal("12 Flower Road, Colombo", confirmed.Address);

        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var stored = await db.Bookings.Include(x => x.Quotation).SingleAsync(x => x.Id == booking.Id);
            Assert.Equal(BookingStatus.Confirmed, stored.Status);
            Assert.Equal(QuotationStatus.Accepted, stored.Quotation.Status);
            Assert.Equal(scenario.RequestId, stored.RequestId);
        });
    }

    [Fact]
    public async Task GetBooking_OtherCustomer_IsForbidden()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        var stranger = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(stranger.AccessToken);

        var response = await client.GetAsync($"/api/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
