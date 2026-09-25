using System.Net;
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
