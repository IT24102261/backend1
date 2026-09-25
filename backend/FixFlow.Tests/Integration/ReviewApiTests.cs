using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Reviews;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Data;
using FixFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class ReviewApiTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task CompletedBooking_CanBeReviewed()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        await scenario.ConfirmAsync(fixture, booking.Id);
        await scenario.CompleteWorkAsync(fixture, booking.Id);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var response = await customer.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 5,
            body = "Switches replaced cleanly."
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<ReviewDto>(FixFlowApiFixture.Json);
        Assert.Equal(5, review!.Rating);
        Assert.Equal("PUBLISHED", review.Status);
        Assert.Equal(scenario.TechnicianProfileId, review.TechnicianId);
    }

    [Fact]
    public async Task PendingBooking_CannotBeReviewed()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var response = await customer.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 4,
            body = "Too early"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateReview_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        await scenario.ConfirmAsync(fixture, booking.Id);
        await scenario.CompleteWorkAsync(fixture, booking.Id);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);
        (await customer.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 5,
            body = "First review"
        }, FixFlowApiFixture.Json)).EnsureSuccessStatusCode();

        var response = await customer.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 4,
            body = "Second review"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRating_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        await scenario.ConfirmAsync(fixture, booking.Id);
        await scenario.CompleteWorkAsync(fixture, booking.Id);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var response = await customer.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 9,
            body = "Impossible rating"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TechnicianSelfReview_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        await scenario.ConfirmAsync(fixture, booking.Id);
        await scenario.CompleteWorkAsync(fixture, booking.Id);

        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var stored = await db.Bookings.Include(x => x.Technician).SingleAsync(x => x.Id == booking.Id);
            stored.CustomerId = stored.Technician.UserId;
            await db.SaveChangesAsync();
        });

        using var technician = fixture.CreateClient(scenario.Technician.AccessToken);
        var asTechnician = await technician.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 5,
            body = "I did a great job"
        }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.Forbidden, asTechnician.StatusCode);

        var customerToken = await fixture.LoginAsync(scenario.Technician.Email);
        using var asSameUser = fixture.CreateClient(customerToken.AccessToken);
        var selfReview = await asSameUser.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 5,
            body = "Self review"
        }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.Forbidden, selfReview.StatusCode);
    }
}
