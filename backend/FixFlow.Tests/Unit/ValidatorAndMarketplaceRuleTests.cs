using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.DTOs.Reviews;
using FixFlow.Application.Validators;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;

namespace FixFlow.Tests.Unit;

public class ValidatorAndMarketplaceRuleTests
{
    [Fact]
    public async Task QuoteValidator_RejectsUnequalTotal()
    {
        var validator = new QuoteWriteRequestValidator();
        var result = await validator.ValidateAsync(new QuoteWriteRequest
        {
            LabourAmount = 10,
            MaterialsAmount = 5,
            TravelAmount = 1,
            TotalAmount = 99,
            Currency = "LKR",
            DurationMinutes = 30,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("Total must equal"));
    }

    [Fact]
    public async Task ReviewValidator_RejectsOutOfRangeRating()
    {
        var validator = new ReviewWriteRequestValidator();
        var result = await validator.ValidateAsync(new ReviewWriteRequest { Rating = 0, Body = "bad" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void StructuredOutput_RejectsInventedCategory()
    {
        var catalog = new List<ServiceCategory> { new() { Id = ServiceCategorySeed.Electrician, Name = "Electrician", IsActive = true } };
        var ex = Assert.Throws<AgentOutputException>(() =>
            AgentOutputValidator.ValidatePlanning(new PlanningOutput { Category = "Wizardry", Confidence = 0.9 }, catalog));
        Assert.Equal("INVENTED_CATEGORY", ex.Code);
    }

    [Fact]
    public void StructuredOutput_NormalizesElectricalAndClampsConfidence()
    {
        var catalog = new List<ServiceCategory> { new() { Id = ServiceCategorySeed.Electrician, Name = "Electrician", IsActive = true } };
        var output = AgentOutputValidator.ValidatePlanning(new PlanningOutput
        {
            Category = "Electrical",
            Confidence = 2,
            Plan = ["Match verified electricians"]
        }, catalog);

        Assert.Equal("Electrician", output.Category);
        Assert.Equal(1, output.Confidence);
        Assert.NotEmpty(output.Plan);
    }

    [Fact]
    public void ServiceArea_MatchesJaffnaTownToDistrict()
    {
        Assert.True(CheckServiceAreaTool.AreaMatches("Jaffna", "kopay"));
        Assert.True(CheckServiceAreaTool.AreaMatches("Jaffna District", "Nallur"));
        Assert.True(CheckServiceAreaTool.AreaMatches("Negombo", "negombo"));
        Assert.True(CheckServiceAreaTool.AreaMatches("Negombo", "Kochchikade"));
        Assert.False(CheckServiceAreaTool.AreaMatches("Jaffna", "Colombo"));
        Assert.False(CheckServiceAreaTool.AreaMatches("Negombo", "Colombo"));
    }

    [Fact]
    public void RequestStateMachine_RejectsInvalidTransition()
    {
        Assert.False(RequestStateMachine.CanTransition(ServiceRequestStatus.Draft, ServiceRequestStatus.Booked));
        Assert.False(RequestStateMachine.CanTransition(ServiceRequestStatus.Completed, ServiceRequestStatus.Draft));
        Assert.True(RequestStateMachine.CanTransition(ServiceRequestStatus.Draft, ServiceRequestStatus.Submitted));
    }

    [Fact]
    public void ServiceRequestRules_AllowsQuotationsOnlyWhileCollecting()
    {
        Assert.True(ServiceRequestRules.AllowsQuotations(ServiceRequestStatus.Matching));
        Assert.True(ServiceRequestRules.AllowsQuotations(ServiceRequestStatus.CollectingQuotes));
        Assert.True(ServiceRequestRules.AllowsQuotations(ServiceRequestStatus.AwaitingCustomerApproval));
        Assert.False(ServiceRequestRules.AllowsQuotations(ServiceRequestStatus.Booked));
    }

    [Fact]
    public async Task RegisterValidator_RejectsEmailWithoutSingleAt()
    {
        var validator = new RegisterRequestValidator();
        var result = await validator.ValidateAsync(new FixFlow.Application.DTOs.Auth.RegisterRequest
        {
            Email = "not-an-email",
            Password = "Password1!",
            DisplayName = "Test",
            Role = "CUSTOMER"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains('@'));
    }

    [Fact]
    public async Task RegisterValidator_RejectsPhoneThatIsNotTenDigits()
    {
        var validator = new RegisterRequestValidator();
        var result = await validator.ValidateAsync(new FixFlow.Application.DTOs.Auth.RegisterRequest
        {
            Email = "ok@fixflow.test",
            Password = "Password1!",
            DisplayName = "Tech",
            Role = "TECHNICIAN",
            Phone = "12345",
            Address = "Nallur",
            CategoryId = ServiceCategorySeed.Electrician
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("10 digits"));
    }

    [Fact]
    public async Task RequestValidator_RejectsPastPreferredStart()
    {
        var validator = new RequestWriteRequestValidator();
        var result = await validator.ValidateAsync(new RequestWriteRequest
        {
            Description = "Need an electrician tomorrow.",
            PreferredStart = DateTimeOffset.UtcNow.AddDays(-1)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("future"));
    }
}
