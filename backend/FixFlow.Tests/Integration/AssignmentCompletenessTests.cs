using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Complaints;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.DTOs.Reporting;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.DTOs.Technicians;
using FixFlow.Tests.Support;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class AssignmentCompletenessTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task Request_PersistsBudgetAndRejectsForeignOwnership()
    {
        var owner = await fixture.RegisterAsync("CUSTOMER");
        var stranger = await fixture.RegisterAsync("CUSTOMER");
        using var ownerClient = fixture.CreateClient(owner.AccessToken);
        var created = await ownerClient.PostAsJsonAsync("/api/requests", new
        {
            description = "Two wall switches are not working. I want both replaced.",
            budgetAmount = 15000m,
            serviceArea = "Colombo"
        }, FixFlowApiFixture.Json);
        created.EnsureSuccessStatusCode();
        var request = await created.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json);
        Assert.Equal(15000m, request!.BudgetAmount);

        using var strangerClient = fixture.CreateClient(stranger.AccessToken);
        var update = await strangerClient.PutAsJsonAsync($"/api/requests/{request.Id}", new { description = "Hijack" }, FixFlowApiFixture.Json);
        var delete = await strangerClient.DeleteAsync($"/api/requests/{request.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Complaint_RequiresOwnedBooking_AndAdminCanResolve()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);
        var select = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);
        select.EnsureSuccessStatusCode();
        var booking = await select.Content.ReadFromJsonAsync<BookingDto>(FixFlowApiFixture.Json);

        var stranger = await fixture.RegisterAsync("CUSTOMER");
        using var strangerClient = fixture.CreateClient(stranger.AccessToken);
        var stolen = await strangerClient.PostAsJsonAsync("/api/complaints", new
        {
            bookingId = booking!.Id,
            subject = "Stolen complaint",
            description = "This should be rejected because I do not own the booking."
        }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.Forbidden, stolen.StatusCode);

        var created = await customer.PostAsJsonAsync("/api/complaints", new
        {
            bookingId = booking.Id,
            subject = "Late arrival",
            description = "Technician did not arrive in the quoted window."
        }, FixFlowApiFixture.Json);
        created.EnsureSuccessStatusCode();
        var complaint = await created.Content.ReadFromJsonAsync<ComplaintDto>(FixFlowApiFixture.Json);

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var resolved = await adminClient.PatchAsJsonAsync($"/api/admin/complaints/{complaint!.Id}/status", new
        {
            status = "RESOLVED",
            resolution = "Refund processed"
        }, FixFlowApiFixture.Json);
        resolved.EnsureSuccessStatusCode();
        var saved = await resolved.Content.ReadFromJsonAsync<ComplaintDto>(FixFlowApiFixture.Json);
        Assert.Equal("RESOLVED", saved!.Status);
        Assert.Equal("Refund processed", saved.Resolution);
    }

    [Fact]
    public async Task ScopeChange_RequiresCustomerDecision()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);
        var select = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);
        select.EnsureSuccessStatusCode();
        var booking = await select.Content.ReadFromJsonAsync<BookingDto>(FixFlowApiFixture.Json);
        await customer.PostAsJsonAsync("/api/bookings/confirm", new { bookingId = booking!.Id }, FixFlowApiFixture.Json);

        using var technician = fixture.CreateClient(scenario.Technician.AccessToken);
        var proposed = await technician.PostAsJsonAsync($"/api/bookings/{booking.Id}/scope-changes", new
        {
            description = "Replace an extra switch",
            proposedCost = 2500m
        }, FixFlowApiFixture.Json);
        proposed.EnsureSuccessStatusCode();
        var change = await proposed.Content.ReadFromJsonAsync<ScopeChangeDto>(FixFlowApiFixture.Json);
        Assert.Equal("PENDING", change!.CustomerDecision);

        var decided = await customer.PostAsJsonAsync($"/api/bookings/scope-changes/{change.Id}/decision", new { decision = "ACCEPTED" }, FixFlowApiFixture.Json);
        decided.EnsureSuccessStatusCode();
        var saved = await decided.Content.ReadFromJsonAsync<ScopeChangeDto>(FixFlowApiFixture.Json);
        Assert.Equal("ACCEPTED", saved!.CustomerDecision);
        Assert.NotNull(saved.DecisionAt);
    }

    [Fact]
    public async Task Admin_CanSuspendTechnician_AndReadAudit()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);

        var suspended = await adminClient.PostAsJsonAsync(
            $"/api/admin/technicians/{scenario.TechnicianProfileId}/suspend",
            new { notes = "Repeated no-shows" },
            FixFlowApiFixture.Json);
        suspended.EnsureSuccessStatusCode();
        var profile = await suspended.Content.ReadFromJsonAsync<TechnicianProfileDto>(FixFlowApiFixture.Json);
        Assert.True(profile!.IsSuspended);

        var audit = await adminClient.GetAsync("/api/admin/audit?page=1&pageSize=20");
        audit.EnsureSuccessStatusCode();
        var page = await audit.Content.ReadFromJsonAsync<PagedAudit>(FixFlowApiFixture.Json);
        Assert.Contains(page!.Items, x => x.Action == "TECHNICIAN_SUSPENDED");

        var dashboard = await adminClient.GetAsync("/api/admin/dashboard");
        dashboard.EnsureSuccessStatusCode();
        var stats = await dashboard.Content.ReadFromJsonAsync<DashboardDto>(FixFlowApiFixture.Json);
        Assert.True(stats!.TotalCustomers >= 1);
        Assert.True(stats.TotalTechnicians >= 1);
    }

    [Fact]
    public async Task PublicTechnician_HidesPrivateFields()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        using var client = fixture.CreateClient();
        var response = await client.GetAsync($"/api/technicians/{scenario.TechnicianProfileId}/public");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sha256", body, StringComparison.OrdinalIgnoreCase);
        var dto = await response.Content.ReadFromJsonAsync<PublicTechnicianDto>(FixFlowApiFixture.Json);
        Assert.Equal("Scenario Technician", dto!.DisplayName);
        Assert.Contains("Electrician", dto.ApprovedCategories);
        Assert.True(dto.CategoryVerified);
    }

    private sealed class PagedAudit
    {
        public List<AuditLogDto> Items { get; set; } = [];
    }
}
