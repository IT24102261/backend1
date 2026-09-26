using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Domain.Constants;
using FixFlow.Infrastructure.Data;
using FixFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class QuotationApiTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task Create_ValidQuote_ReturnsSentVersionOne()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture, createQuote: false);

        var quote = await scenario.CreateQuoteAsync(fixture);

        Assert.Equal("SENT", quote.Status);
        Assert.Equal(1, quote.Version);
        Assert.Equal(6000m, quote.TotalAmount);
        Assert.Equal(scenario.RequestId, quote.RequestId);
    }

    [Fact]
    public async Task Create_MismatchedTotal_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture, createQuote: false);
        using var tech = fixture.CreateClient(scenario.Technician.AccessToken);

        var response = await tech.PostAsJsonAsync($"/api/invitations/{scenario.InvitationId}/quote", new
        {
            labourAmount = 4000m,
            materialsAmount = 1500m,
            travelAmount = 500m,
            totalAmount = 9999m,
            currency = "LKR",
            durationMinutes = 60,
            expiresAt = DateTimeOffset.UtcNow.AddDays(2)
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Select_ExpiredQuote_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        await scenario.ExpireQuoteAsync(fixture, scenario.QuoteId!.Value);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var response = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ListQuotes_OtherTechnician_DoesNotSeeSubmittedQuote()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var other = await fixture.RegisterAsync("TECHNICIAN");
        using var otherClient = fixture.CreateClient(other.AccessToken);
        await otherClient.PutAsJsonAsync("/api/technicians/profile", new { serviceArea = "Jaffna" }, FixFlowApiFixture.Json);

        var list = await otherClient.GetAsync($"/api/requests/{scenario.RequestId}/quotes");
        list.EnsureSuccessStatusCode();
        var quotes = await list.Content.ReadFromJsonAsync<QuoteDto[]>(FixFlowApiFixture.Json);

        Assert.NotNull(quotes);
        Assert.Empty(quotes);

        var peek = await otherClient.GetAsync($"/api/quotes/{scenario.QuoteId}");
        Assert.Equal(HttpStatusCode.Forbidden, peek.StatusCode);
    }

    [Fact]
    public async Task Create_UninvitedTechnician_IsForbidden()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture, createQuote: false);
        var other = await fixture.RegisterAsync("TECHNICIAN");
        using var otherClient = fixture.CreateClient(other.AccessToken);
        await otherClient.PutAsJsonAsync("/api/technicians/profile", new { serviceArea = "Colombo" }, FixFlowApiFixture.Json);

        var response = await otherClient.PostAsJsonAsync($"/api/invitations/{scenario.InvitationId}/quote", new
        {
            labourAmount = 1000m,
            materialsAmount = 0m,
            travelAmount = 0m,
            totalAmount = 1000m,
            currency = "LKR",
            durationMinutes = 45,
            expiresAt = DateTimeOffset.UtcNow.AddDays(1)
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutExistingWorkflow_StillReturnsSent()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture, createQuote: false);
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            db.AiWorkflows.RemoveRange(db.AiWorkflows.Where(x => x.RequestId == scenario.RequestId));
            await db.SaveChangesAsync();
        });

        var quote = await scenario.CreateQuoteAsync(fixture);

        Assert.Equal("SENT", quote.Status);
        Assert.Equal(scenario.RequestId, quote.RequestId);
    }

    [Fact]
    public async Task Create_RevisedQuote_IncrementsVersionAndWithdrawsPrevious()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var firstId = scenario.QuoteId!.Value;

        var revised = await scenario.CreateQuoteAsync(fixture);

        Assert.Equal(2, revised.Version);
        Assert.Equal(scenario.RequestId, revised.RequestId);
        Assert.NotEqual(firstId, revised.Id);
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var previous = await db.Quotations.SingleAsync(x => x.Id == firstId);
            var current = await db.Quotations.SingleAsync(x => x.Id == revised.Id);
            Assert.Equal("WITHDRAWN", FixFlow.Application.Mapping.EnumMap.ToApi(previous.Status));
            Assert.Equal(previous.QuoteGroupId, current.QuoteGroupId);
            Assert.Equal(2, current.Version);
        });
    }

    [Fact]
    public async Task Create_PastExpiry_IsRejectedByValidator()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture, createQuote: false);
        using var tech = fixture.CreateClient(scenario.Technician.AccessToken);

        var response = await tech.PostAsJsonAsync($"/api/invitations/{scenario.InvitationId}/quote", new
        {
            labourAmount = 1000m,
            materialsAmount = 0m,
            travelAmount = 0m,
            totalAmount = 1000m,
            currency = "LKR",
            durationMinutes = 30,
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnapprovedCategory_CannotCreateQuote()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var tech = fixture.CreateClient(technician.AccessToken);
        await tech.PutAsJsonAsync("/api/technicians/profile", new { serviceArea = "Colombo" }, FixFlowApiFixture.Json);
        var apply = await tech.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Electrician
        }, FixFlowApiFixture.Json);
        apply.EnsureSuccessStatusCode();

        Guid invitationId = Guid.Empty;
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var profile = await db.TechnicianProfiles.SingleAsync(x => x.UserId == technician.UserId);
            var request = new FixFlow.Domain.Entities.ServiceRequest
            {
                CustomerId = customer.UserId,
                CategoryId = ServiceCategorySeed.Plumber,
                Description = "Leaking kitchen tap needs a plumber.",
                ServiceArea = "Colombo",
                Status = FixFlow.Domain.Enums.ServiceRequestStatus.CollectingQuotes
            };
            db.ServiceRequests.Add(request);
            var invitation = new FixFlow.Domain.Entities.RequestInvitation
            {
                RequestId = request.Id,
                TechnicianId = profile.Id
            };
            db.RequestInvitations.Add(invitation);
            await db.SaveChangesAsync();
            invitationId = invitation.Id;
        });

        var response = await tech.PostAsJsonAsync($"/api/invitations/{invitationId}/quote", new
        {
            labourAmount = 2000m,
            materialsAmount = 0m,
            travelAmount = 0m,
            totalAmount = 2000m,
            currency = "LKR",
            durationMinutes = 40,
            expiresAt = DateTimeOffset.UtcNow.AddDays(1)
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SecondInvitedTechnician_CanQuoteAfterFirstQuote()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        var other = await fixture.RegisterAsync("TECHNICIAN", displayName: "Second Technician");
        using var otherClient = fixture.CreateClient(other.AccessToken);
        await otherClient.PutAsJsonAsync("/api/technicians/profile", new { serviceArea = "Colombo" }, FixFlowApiFixture.Json);
        var apply = await otherClient.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Electrician
        }, FixFlowApiFixture.Json);
        apply.EnsureSuccessStatusCode();
        var application = await apply.Content.ReadFromJsonAsync<FixFlow.Application.DTOs.Technicians.TechnicianApplicationDto>(FixFlowApiFixture.Json);
        using var admin = fixture.CreateClient(scenario.Admin.AccessToken);
        (await admin.PostAsJsonAsync(
            $"/api/admin/technician-applications/{application!.Id}/approve",
            new { notes = "Verified" },
            FixFlowApiFixture.Json)).EnsureSuccessStatusCode();

        Guid invitationId = Guid.Empty;
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var profile = await db.TechnicianProfiles.SingleAsync(x => x.UserId == other.UserId);
            var invitation = new FixFlow.Domain.Entities.RequestInvitation
            {
                RequestId = scenario.RequestId,
                TechnicianId = profile.Id
            };
            db.RequestInvitations.Add(invitation);
            await db.SaveChangesAsync();
            invitationId = invitation.Id;
        });

        var response = await otherClient.PostAsJsonAsync($"/api/invitations/{invitationId}/quote", new
        {
            labourAmount = 3500m,
            materialsAmount = 1000m,
            travelAmount = 500m,
            totalAmount = 5000m,
            currency = "LKR",
            durationMinutes = 45,
            expiresAt = DateTimeOffset.UtcNow.AddDays(2)
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quotes = await fixture.CreateClient(scenario.Customer.AccessToken)
            .GetFromJsonAsync<QuoteDto[]>($"/api/requests/{scenario.RequestId}/quotes", FixFlowApiFixture.Json);
        Assert.NotNull(quotes);
        Assert.Equal(2, quotes.Count(x => x.Status == "SENT"));
    }

    [Fact]
    public async Task Create_AfterQuoteSelected_IsRejected()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        await scenario.SelectQuoteAsync(fixture);
        using var tech = fixture.CreateClient(scenario.Technician.AccessToken);

        var response = await tech.PostAsJsonAsync($"/api/invitations/{scenario.InvitationId}/quote", new
        {
            labourAmount = 4000m,
            materialsAmount = 1500m,
            travelAmount = 500m,
            totalAmount = 6000m,
            currency = "LKR",
            durationMinutes = 60,
            expiresAt = DateTimeOffset.UtcNow.AddDays(2)
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
