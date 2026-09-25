using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Complaints;
using FixFlow.Application.DTOs.Reviews;
using FixFlow.Tests.Support;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class AdminModerationTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task Admin_CanReplyEditAndDeleteReview()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var booking = await scenario.SelectQuoteAsync(fixture);
        await scenario.ConfirmAsync(fixture, booking.Id);
        await scenario.CompleteWorkAsync(fixture, booking.Id);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);
        var created = await customer.PostAsJsonAsync($"/api/bookings/{booking.Id}/reviews", new
        {
            rating = 4,
            body = "Good work overall."
        }, FixFlowApiFixture.Json);
        created.EnsureSuccessStatusCode();
        var review = await created.Content.ReadFromJsonAsync<ReviewDto>(FixFlowApiFixture.Json);

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var replied = await adminClient.PostAsJsonAsync($"/api/admin/reviews/{review!.Id}/reply", new { reply = "Thank you for the feedback." }, FixFlowApiFixture.Json);
        replied.EnsureSuccessStatusCode();
        var afterReply = await replied.Content.ReadFromJsonAsync<ReviewDto>(FixFlowApiFixture.Json);
        Assert.Equal("Thank you for the feedback.", afterReply!.AdminReply);

        var edited = await adminClient.PatchAsJsonAsync($"/api/admin/reviews/{review.Id}", new { rating = 5, body = "Edited by admin." }, FixFlowApiFixture.Json);
        edited.EnsureSuccessStatusCode();
        var afterEdit = await edited.Content.ReadFromJsonAsync<ReviewDto>(FixFlowApiFixture.Json);
        Assert.Equal(5, afterEdit!.Rating);
        Assert.Equal("Edited by admin.", afterEdit.Body);

        var deleted = await adminClient.DeleteAsync($"/api/admin/reviews/{review.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Admin_CanReplyToComplaint()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);
        var select = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);
        select.EnsureSuccessStatusCode();
        var booking = await select.Content.ReadFromJsonAsync<FixFlow.Application.DTOs.Quotes.BookingDto>(FixFlowApiFixture.Json);
        var created = await customer.PostAsJsonAsync("/api/complaints", new
        {
            bookingId = booking!.Id,
            subject = "Late arrival",
            description = "Technician arrived after the quoted window."
        }, FixFlowApiFixture.Json);
        created.EnsureSuccessStatusCode();
        var complaint = await created.Content.ReadFromJsonAsync<ComplaintDto>(FixFlowApiFixture.Json);

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var replied = await adminClient.PostAsJsonAsync($"/api/admin/complaints/{complaint!.Id}/reply", new
        {
            reply = "We are reviewing this with the technician."
        }, FixFlowApiFixture.Json);
        replied.EnsureSuccessStatusCode();
        var saved = await replied.Content.ReadFromJsonAsync<ComplaintDto>(FixFlowApiFixture.Json);
        Assert.Equal("We are reviewing this with the technician.", saved!.AdminReply);
        Assert.Equal("IN_REVIEW", saved.Status);
    }

    [Fact]
    public async Task Admin_CanCreateCustomerAndTechnician()
    {
        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);

        var customerEmail = $"admin.customer.{Guid.NewGuid():N}@fixflow.test";
        var customer = await adminClient.PostAsJsonAsync("/api/admin/users", new
        {
            email = customerEmail,
            password = "Password1!",
            displayName = "Admin Created Customer",
            role = "CUSTOMER"
        }, FixFlowApiFixture.Json);
        customer.EnsureSuccessStatusCode();
        var customerUser = await customer.Content.ReadFromJsonAsync<AdminUserDto>(FixFlowApiFixture.Json);
        Assert.Equal("CUSTOMER", customerUser!.Role);
        Assert.Null(customerUser.TechnicianProfileId);

        var technicianEmail = $"admin.tech.{Guid.NewGuid():N}@fixflow.test";
        var technician = await adminClient.PostAsJsonAsync("/api/admin/users", new
        {
            email = technicianEmail,
            password = "Password1!",
            displayName = "Admin Created Technician",
            role = "TECHNICIAN",
            serviceArea = "Jaffna"
        }, FixFlowApiFixture.Json);
        technician.EnsureSuccessStatusCode();
        var technicianUser = await technician.Content.ReadFromJsonAsync<AdminUserDto>(FixFlowApiFixture.Json);
        Assert.Equal("TECHNICIAN", technicianUser!.Role);
        Assert.NotNull(technicianUser.TechnicianProfileId);
        Assert.Equal("Jaffna", technicianUser.ServiceArea);

        var forbidden = await adminClient.PostAsJsonAsync("/api/admin/users", new
        {
            email = $"admin.blocked.{Guid.NewGuid():N}@fixflow.test",
            password = "Password1!",
            displayName = "Blocked Admin",
            role = "ADMIN"
        }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}
