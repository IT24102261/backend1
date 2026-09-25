using FixFlow.Application.Agents;
using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAgentToolExecutor, AgentToolExecutor>();
        services.AddScoped<RequestPlanningAgent>();
        services.AddScoped<TechnicianMatchingAgent>();
        services.AddScoped<QuotationRecommendationAgent>();
        services.AddScoped<BookingValidationAgent>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<ITechnicianService, TechnicianService>();
        services.AddScoped<IMarketplaceService, MarketplaceService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IAiWorkflowService, AiWorkflowService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IAgentTool, GetServiceCategoriesTool>();
        services.AddScoped<IAgentTool, GetRequestTool>();
        services.AddScoped<IAgentTool, GetCategoryRequirementsTool>();
        services.AddScoped<IAgentTool, GetApprovedTechniciansByCategoryTool>();
        services.AddScoped<IAgentTool, CheckTechnicianActiveTool>();
        services.AddScoped<IAgentTool, CheckServiceAreaTool>();
        services.AddScoped<IAgentTool, CheckAvailabilityTool>();
        services.AddScoped<IAgentTool, CheckCapacityTool>();
        services.AddScoped<IAgentTool, GetSkillTagsTool>();
        services.AddScoped<IAgentTool, GetValidQuotationsTool>();
        services.AddScoped<IAgentTool, GetTechnicianReputationTool>();
        services.AddScoped<IAgentTool, GetCompletedJobCountTool>();
        services.AddScoped<IAgentTool, GetDistanceBandTool>();
        services.AddScoped<IAgentTool, GetCustomerPreferencesTool>();
        services.AddScoped<IAgentTool, GetQuotationTool>();
        services.AddScoped<IAgentTool, CheckQuoteExpiryTool>();
        services.AddScoped<IAgentTool, CheckCustomerOwnershipTool>();
        services.AddScoped<IAgentTool, CheckCategoryApprovalTool>();
        services.AddScoped<IAgentTool, CheckTechnicianAvailabilityTool>();
        services.AddScoped<IAgentTool, CheckBookingConflictTool>();
        services.AddScoped<IAgentTool, CheckQuoteRequestRelationshipTool>();
        services.AddScoped<IAgentTool, CheckApprovalTool>();
        services.AddScoped<IAgentTool, GetRequestForValidationTool>();
        return services;
    }
}
