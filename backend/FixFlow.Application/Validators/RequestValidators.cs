using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Auth;
using FixFlow.Application.DTOs.Categories;
using FixFlow.Application.DTOs.Complaints;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.DTOs.Reviews;
using FixFlow.Application.DTOs.Technicians;
using FluentValidation;

namespace FixFlow.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40).When(IsTechnician);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(400).When(IsTechnician);
        RuleFor(x => x.CategoryId).NotEmpty().When(IsTechnician);
    }

    private static bool IsTechnician(RegisterRequest request) =>
        string.Equals(request.Role, "TECHNICIAN", StringComparison.OrdinalIgnoreCase);
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CategoryWriteRequestValidator : AbstractValidator<CategoryWriteRequest>
{
    public CategoryWriteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class RequestWriteRequestValidator : AbstractValidator<RequestWriteRequest>
{
    public RequestWriteRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.BudgetAmount).GreaterThan(0).When(x => x.BudgetAmount.HasValue);
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ScopeChangeWriteRequestValidator : AbstractValidator<ScopeChangeWriteRequest>
{
    public ScopeChangeWriteRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ProposedCost).GreaterThanOrEqualTo(0);
    }
}

public class QuoteWriteRequestValidator : AbstractValidator<QuoteWriteRequest>
{
    public QuoteWriteRequestValidator()
    {
        RuleFor(x => x.LabourAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaterialsAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TravelAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow).When(x => x.ExpiresAt.HasValue);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x).Must(x => x.LabourAmount + x.MaterialsAmount + x.TravelAmount == x.TotalAmount)
            .WithMessage("Total must equal labour + materials + travel.");
    }
}

public class ReviewWriteRequestValidator : AbstractValidator<ReviewWriteRequest>
{
    public ReviewWriteRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Body).MaximumLength(2000);
    }
}

public class ComplaintWriteRequestValidator : AbstractValidator<ComplaintWriteRequest>
{
    public ComplaintWriteRequestValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
    }
}

public class TechnicianApplicationRequestValidator : AbstractValidator<TechnicianApplicationRequest>
{
    public TechnicianApplicationRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class ClarificationRequestValidator : AbstractValidator<ClarificationRequest>
{
    public ClarificationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}

public class AdminReplyRequestValidator : AbstractValidator<AdminReplyRequest>
{
    public AdminReplyRequestValidator()
    {
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(2000);
    }
}

public class AdminReviewUpdateRequestValidator : AbstractValidator<AdminReviewUpdateRequest>
{
    public AdminReviewUpdateRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Body).MaximumLength(2000);
    }
}

public class AdminCreateUserRequestValidator : AbstractValidator<AdminCreateUserRequest>
{
    public AdminCreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.ServiceArea).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
