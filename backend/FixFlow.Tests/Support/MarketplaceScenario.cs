using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Auth;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.DTOs.Technicians;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Tests.Support;

public sealed class MarketplaceScenario
{
    public required AuthResponse Customer { get; init; }
    public required AuthResponse Technician { get; init; }
    public required AuthResponse Admin { get; init; }
    public required Guid TechnicianProfileId { get; init; }
    public required Guid ApplicationId { get; init; }
    public Guid? PlumberApplicationId { get; init; }
    public required Guid RequestId { get; init; }
    public required Guid InvitationId { get; init; }
    public Guid? QuoteId { get; set; }

    public static async Task<MarketplaceScenario> CreateAsync(
        FixFlowApiFixture fixture,
        bool createQuote = true,
        DateTimeOffset? arrivalStart = null,
        DateTimeOffset? expiresAt = null)
    {
        var customer = await fixture.RegisterAsync("CUSTOMER", displayName: "Scenario Customer");
        var technician = await fixture.RegisterAsync("TECHNICIAN", displayName: "Scenario Technician");
        var admin = await fixture.LoginAdminAsync();

        using var techClient = fixture.CreateClient(technician.AccessToken);
        var profileResponse = await techClient.PutAsJsonAsync("/api/technicians/profile", new
        {
            bio = "Licensed electrician",
            serviceArea = "Colombo",
            latitudeApprox = 6.9271,
            longitudeApprox = 79.8612,
            experienceSummary = "Switch replacement"
        }, FixFlowApiFixture.Json);
        profileResponse.EnsureSuccessStatusCode();
        var profile = (await profileResponse.Content.ReadFromJsonAsync<TechnicianProfileDto>(FixFlowApiFixture.Json))!;

        var applyResponse = await techClient.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Electrician
        }, FixFlowApiFixture.Json);
        applyResponse.EnsureSuccessStatusCode();
        var application = (await applyResponse.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json))!;

        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var approve = await adminClient.PostAsJsonAsync(
            $"/api/admin/technician-applications/{application.Id}/approve",
            new { notes = "Documents verified" },
            FixFlowApiFixture.Json);
        approve.EnsureSuccessStatusCode();

        using var customerClient = fixture.CreateClient(customer.AccessToken);
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var createRequest = await customerClient.PostAsJsonAsync("/api/requests", new
        {
            categoryId = ServiceCategorySeed.Electrician,
            description = "Two wall switches are not working. I want them replaced.",
            preferredStart = start,
            preferredEnd = start.AddHours(2),
            serviceArea = "Colombo",
            address = "12 Flower Road, Colombo",
            latitude = 6.9271,
            longitude = 79.8612
        }, FixFlowApiFixture.Json);
        createRequest.EnsureSuccessStatusCode();
        var request = (await createRequest.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json))!;

        var invitationId = await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var stored = await db.ServiceRequests.SingleAsync(x => x.Id == request.Id);
            stored.Status = ServiceRequestStatus.CollectingQuotes;
            stored.CategoryId = ServiceCategorySeed.Electrician;
            var invitation = new RequestInvitation
            {
                RequestId = stored.Id,
                TechnicianId = profile.Id,
                Status = InvitationStatus.Sent
            };
            db.RequestInvitations.Add(invitation);
            db.AiWorkflows.Add(new AiWorkflow
            {
                RequestId = stored.Id,
                Objective = "Plan, match, recommend, and validate a home-service booking.",
                Status = AiWorkflowStatus.QuoteCollection,
                CurrentStep = "QUOTE_COLLECTION",
                PlanJson = "{}",
                CompletedStepsJson = "[\"PLANNING\",\"MATCHING\",\"QUOTE_COLLECTION\"]"
            });
            await db.SaveChangesAsync();
            return invitation.Id;
        });

        var scenario = new MarketplaceScenario
        {
            Customer = customer,
            Technician = technician,
            Admin = admin,
            TechnicianProfileId = profile.Id,
            ApplicationId = application.Id,
            RequestId = request.Id,
            InvitationId = invitationId
        };

        if (createQuote)
        {
            await scenario.CreateQuoteAsync(fixture, arrivalStart, expiresAt);
        }

        return scenario;
    }

    public async Task<QuoteDto> CreateQuoteAsync(
        FixFlowApiFixture fixture,
        DateTimeOffset? arrivalStart = null,
        DateTimeOffset? expiresAt = null)
    {
        using var techClient = fixture.CreateClient(Technician.AccessToken);
        var response = await techClient.PostAsJsonAsync($"/api/invitations/{InvitationId}/quote", new
        {
            labourAmount = 4000m,
            materialsAmount = 1500m,
            travelAmount = 500m,
            totalAmount = 6000m,
            currency = "LKR",
            durationMinutes = 60,
            arrivalStart = arrivalStart ?? DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            expiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(2),
            assumptions = "Standard switch replacement"
        }, FixFlowApiFixture.Json);
        response.EnsureSuccessStatusCode();
        var quote = (await response.Content.ReadFromJsonAsync<QuoteDto>(FixFlowApiFixture.Json))!;
        QuoteId = quote.Id;
        return quote;
    }

    public async Task<BookingDto> SelectQuoteAsync(FixFlowApiFixture fixture, Guid? quoteId = null)
    {
        using var customerClient = fixture.CreateClient(Customer.AccessToken);
        var response = await customerClient.PostAsync($"/api/quotes/{quoteId ?? QuoteId}/select", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>(FixFlowApiFixture.Json))!;
    }

    public async Task<BookingDto> ConfirmAsync(FixFlowApiFixture fixture, Guid bookingId)
    {
        using var customerClient = fixture.CreateClient(Customer.AccessToken);
        var response = await customerClient.PostAsJsonAsync("/api/bookings/confirm", new { bookingId }, FixFlowApiFixture.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>(FixFlowApiFixture.Json))!;
    }

    public async Task<BookingDto> ChangeStatusAsync(FixFlowApiFixture fixture, Guid bookingId, string status, string accessToken)
    {
        using var client = fixture.CreateClient(accessToken);
        var response = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}/status", new { status, note = status }, FixFlowApiFixture.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>(FixFlowApiFixture.Json))!;
    }

    public async Task<BookingDto> CompleteWorkAsync(FixFlowApiFixture fixture, Guid bookingId)
    {
        await ChangeStatusAsync(fixture, bookingId, "ACCEPTED", Technician.AccessToken);
        await ChangeStatusAsync(fixture, bookingId, "EN_ROUTE", Technician.AccessToken);
        await ChangeStatusAsync(fixture, bookingId, "IN_PROGRESS", Technician.AccessToken);
        await ChangeStatusAsync(fixture, bookingId, "WORK_COMPLETED", Technician.AccessToken);
        return await ChangeStatusAsync(fixture, bookingId, "CUSTOMER_CONFIRMED", Customer.AccessToken);
    }

    public async Task ExpireQuoteAsync(FixFlowApiFixture fixture, Guid quoteId)
    {
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var quote = await db.Quotations.SingleAsync(x => x.Id == quoteId);
            quote.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        });
    }

    public async Task SuspendTechnicianAsync(FixFlowApiFixture fixture)
    {
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var profile = await db.TechnicianProfiles.SingleAsync(x => x.Id == TechnicianProfileId);
            profile.IsSuspended = true;
            await db.SaveChangesAsync();
        });
    }

    public static async Task<HttpResponseMessage> PostEmptyMediaAsync(HttpClient client, Guid requestId)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent([]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "empty.jpg");
        return await client.PostAsync($"/api/requests/{requestId}/media", content);
    }

    public static async Task<HttpResponseMessage> PostJpegAsync(HttpClient client, Guid requestId)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xD9]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "switch.jpg");
        return await client.PostAsync($"/api/requests/{requestId}/media", content);
    }

    public static async Task<HttpResponseMessage> PostEvidenceAsync(HttpClient client, Guid applicationId)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent("license-scan"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "license.pdf");
        content.Add(new StringContent("LICENSE"), "evidenceType");
        return await client.PostAsync($"/api/technician-applications/{applicationId}/documents", content);
    }
}
