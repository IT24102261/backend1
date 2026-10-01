using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Technicians;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Data;
using FixFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class TechnicianVerificationApiTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task SubmitApplication_CreatesSubmittedCategoryApplication()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var client = fixture.CreateClient(technician.AccessToken);
        await client.PutAsJsonAsync("/api/technicians/profile", new { serviceArea = "Colombo" }, FixFlowApiFixture.Json);

        var response = await client.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Electrician
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var application = await response.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json);
        Assert.Equal("SUBMITTED", application!.Status);
        Assert.Equal(ServiceCategorySeed.Electrician, application.CategoryId);
    }

    [Fact]
    public async Task UploadEvidence_StoresDocumentAgainstApplication()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var client = fixture.CreateClient(technician.AccessToken);
        var apply = await client.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Plumber
        }, FixFlowApiFixture.Json);
        apply.EnsureSuccessStatusCode();
        var application = (await apply.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json))!;

        var response = await MarketplaceScenario.PostEvidenceAsync(client, application.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentDto>(FixFlowApiFixture.Json);
        Assert.Equal("LICENSE", document!.EvidenceType);
        Assert.Equal("application/pdf", document.MimeType);
        Assert.False(string.IsNullOrWhiteSpace(document.Url));
    }

    [Fact]
    public async Task AdminGet_IncludesUploadedEvidenceAndServesFile()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var client = fixture.CreateClient(technician.AccessToken);
        var apply = await client.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Plumber
        }, FixFlowApiFixture.Json);
        var application = (await apply.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json))!;
        (await MarketplaceScenario.PostEvidenceAsync(client, application.Id)).EnsureSuccessStatusCode();

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var detail = await adminClient.GetFromJsonAsync<TechnicianApplicationDto>(
            $"/api/admin/technician-applications/{application.Id}",
            FixFlowApiFixture.Json);

        Assert.NotNull(detail);
        Assert.Equal(1, detail!.EvidenceCount);
        Assert.Single(detail.Documents);
        Assert.Equal("LICENSE", detail.Documents[0].EvidenceType);

        var file = await adminClient.GetAsync(detail.Documents[0].Url);
        file.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", file.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminApproval_MarksApplicationApproved()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var techClient = fixture.CreateClient(technician.AccessToken);
        var apply = await techClient.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Solar
        }, FixFlowApiFixture.Json);
        var application = (await apply.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json))!;

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/technician-applications/{application.Id}/approve",
            new { notes = "License checked" },
            FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decided = await response.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json);
        Assert.Equal("APPROVED", decided!.Status);
    }

    [Fact]
    public async Task UnauthorizedApproval_IsForbidden()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var techClient = fixture.CreateClient(technician.AccessToken);
        var apply = await techClient.PostAsJsonAsync("/api/technician-applications", new
        {
            categoryId = ServiceCategorySeed.Painter
        }, FixFlowApiFixture.Json);
        var application = (await apply.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json))!;

        var response = await techClient.PostAsJsonAsync(
            $"/api/admin/technician-applications/{application.Id}/approve",
            new { notes = "self approve" },
            FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await fixture.ScopeAsync(async services =>
        {
            var db = services.GetRequiredService<FixFlowDbContext>();
            var stored = await db.TechnicianCategoryApplications.SingleAsync(x => x.Id == application.Id);
            Assert.Equal(ApplicationStatus.Submitted, stored.Status);
        });
    }

    [Fact]
    public async Task CategoryApproval_IsIndependentPerCategory()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var techClient = fixture.CreateClient(technician.AccessToken);
        var electrician = await ApplyAsync(techClient, ServiceCategorySeed.Electrician);
        var plumber = await ApplyAsync(techClient, ServiceCategorySeed.Plumber);

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        (await adminClient.PostAsJsonAsync(
            $"/api/admin/technician-applications/{electrician.Id}/approve",
            new { notes = "Electrician only" },
            FixFlowApiFixture.Json)).EnsureSuccessStatusCode();

        var electricianStatus = await techClient.GetFromJsonAsync<TechnicianApplicationDto>(
            $"/api/technician-applications/{electrician.Id}", FixFlowApiFixture.Json);
        var plumberStatus = await techClient.GetFromJsonAsync<TechnicianApplicationDto>(
            $"/api/technician-applications/{plumber.Id}", FixFlowApiFixture.Json);

        Assert.Equal("APPROVED", electricianStatus!.Status);
        Assert.Equal("SUBMITTED", plumberStatus!.Status);
    }

    [Fact]
    public async Task AdminCanSetTechnicianProfilePhoto()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var techClient = fixture.CreateClient(technician.AccessToken);
        var profile = await techClient.GetFromJsonAsync<TechnicianProfileDto>("/api/technicians/profile", FixFlowApiFixture.Json);

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        using var content = new MultipartFormDataContent();
        var photo = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xD9]);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(photo, "file", "admin-photo.jpg");
        var response = await adminClient.PostAsync($"/api/admin/technicians/{profile!.Id}/photo", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = await techClient.GetFromJsonAsync<TechnicianProfileDto>("/api/technicians/profile", FixFlowApiFixture.Json);
        Assert.False(string.IsNullOrWhiteSpace(mine!.ProfilePhotoUrl));

        var anonymous = fixture.CreateClient();
        var photoResponse = await anonymous.GetAsync($"/api/technicians/{profile.Id}/photo");
        Assert.Equal(HttpStatusCode.OK, photoResponse.StatusCode);

        using var ownContent = new MultipartFormDataContent();
        var replacement = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        replacement.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        ownContent.Add(replacement, "file", "mine.png");
        var ownResponse = await techClient.PostAsync("/api/technicians/profile/photo", ownContent);
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);

        var updated = await techClient.GetFromJsonAsync<TechnicianProfileDto>("/api/technicians/profile", FixFlowApiFixture.Json);
        Assert.False(string.IsNullOrWhiteSpace(updated!.ProfilePhotoUrl));
        Assert.NotEqual(mine.ProfilePhotoUrl, updated.ProfilePhotoUrl);
    }

    [Fact]
    public async Task SuspendedTechnician_CannotBeBooked()
    {
        var scenario = await MarketplaceScenario.CreateAsync(fixture);
        await scenario.SuspendTechnicianAsync(fixture);
        using var customer = fixture.CreateClient(scenario.Customer.AccessToken);

        var response = await customer.PostAsync($"/api/quotes/{scenario.QuoteId}/select", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<TechnicianApplicationDto> ApplyAsync(HttpClient client, Guid categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/technician-applications", new { categoryId }, FixFlowApiFixture.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TechnicianApplicationDto>(FixFlowApiFixture.Json))!;
    }
}
