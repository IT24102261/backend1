using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Domain.Constants;
using FixFlow.Tests.Support;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class CustomerRequestApiTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task Create_PersistsDraftForCustomer()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);

        var response = await client.PostAsJsonAsync("/api/requests", NewRequest("Kitchen outlet is sparking and needs replacement."), FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = await response.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json);
        Assert.NotNull(request);
        Assert.Equal(customer.UserId, request.CustomerId);
        Assert.Equal("DRAFT", request.Status);
        Assert.Equal("12 Flower Road, Colombo", request.Address);
    }

    [Fact]
    public async Task Update_OwnDraft_Succeeds()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);
        var created = await CreateDraftAsync(client, "Original description of the switch.");

        var response = await client.PutAsJsonAsync($"/api/requests/{created.Id}", NewRequest("Updated: two wall switches are not working."), FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json);
        Assert.Contains("Updated", updated!.Description);
    }

    [Fact]
    public async Task Update_AnotherCustomersRequest_IsForbidden()
    {
        var owner = await fixture.RegisterAsync("CUSTOMER");
        var stranger = await fixture.RegisterAsync("CUSTOMER");
        using var ownerClient = fixture.CreateClient(owner.AccessToken);
        var created = await CreateDraftAsync(ownerClient, "Owner draft for a leaking tap.");

        using var strangerClient = fixture.CreateClient(stranger.AccessToken);
        var response = await strangerClient.PutAsJsonAsync($"/api/requests/{created.Id}", NewRequest("Hijacked description of the job."), FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_MovesDraftThroughAnalysis()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);
        var created = await CreateDraftAsync(client, "Two wall switches are not working. I want them replaced.");

        var response = await client.PostAsync($"/api/requests/{created.Id}/submit", null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var submitted = await response.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json);
        Assert.NotNull(submitted);
        Assert.NotEqual("DRAFT", submitted.Status);
        Assert.Contains(submitted.Status, new[] { "ANALYZING", "MATCHING", "COLLECTING_QUOTES", "CLARIFICATION_REQUIRED", "FAILED", "SUBMITTED" });

        var again = await client.PostAsync($"/api/requests/{created.Id}/submit", null);
        Assert.True(again.IsSuccessStatusCode, await again.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Update_AfterSubmit_IsInvalidStateTransition()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);
        var created = await CreateDraftAsync(client, "Two wall switches are not working. I want them replaced.");
        (await client.PostAsync($"/api/requests/{created.Id}/submit", null)).EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync($"/api/requests/{created.Id}", NewRequest("Trying to edit after submit."), FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Media_EmptyFile_IsRejected()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);
        var created = await CreateDraftAsync(client, "Need photos of the broken switch.");

        var response = await MarketplaceScenario.PostEmptyMediaAsync(client, created.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Media_ValidJpeg_IsAccepted()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);
        var created = await CreateDraftAsync(client, "Need photos of the broken switch.");

        var response = await MarketplaceScenario.PostJpegAsync(client, created.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var media = await response.Content.ReadFromJsonAsync<MediaDto>(FixFlowApiFixture.Json);
        Assert.Equal("image/jpeg", media!.MimeType);
        Assert.False(string.IsNullOrWhiteSpace(media.StorageKey));
    }

    [Fact]
    public async Task Cancel_NotifiesInvitedAndQuotingTechnicians()
    {
        var scene = await MarketplaceScenario.CreateAsync(fixture);
        using var customer = fixture.CreateClient(scene.Customer.AccessToken);
        var response = await customer.PostAsync($"/api/requests/{scene.RequestId}/cancel", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var cancelled = await response.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json);
        Assert.Equal("CANCELLED", cancelled!.Status);

        using var technician = fixture.CreateClient(scene.Technician.AccessToken);
        var notes = await technician.GetFromJsonAsync<List<FixFlow.Application.DTOs.Notifications.NotificationDto>>(
            "/api/notifications",
            FixFlowApiFixture.Json);
        Assert.Contains(notes!, item => item.Message.Contains("cancelled by the customer", StringComparison.OrdinalIgnoreCase));

        var invites = await technician.GetFromJsonAsync<List<FixFlow.Application.DTOs.Quotes.InvitationDto>>(
            "/api/invitations",
            FixFlowApiFixture.Json);
        Assert.DoesNotContain(invites!, item => item.RequestId == scene.RequestId);
    }

    private static async Task<RequestDto> CreateDraftAsync(HttpClient client, string description)
    {
        var response = await client.PostAsJsonAsync("/api/requests", NewRequest(description), FixFlowApiFixture.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RequestDto>(FixFlowApiFixture.Json))!;
    }

    private static object NewRequest(string description) => new
    {
        categoryId = ServiceCategorySeed.Electrician,
        description,
        serviceArea = "Colombo",
        address = "12 Flower Road, Colombo",
        latitude = 6.9271,
        longitude = 79.8612,
        preferredStart = DateTimeOffset.UtcNow.AddDays(1),
        preferredEnd = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
    };
}
