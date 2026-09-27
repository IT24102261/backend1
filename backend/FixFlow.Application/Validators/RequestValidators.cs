using FixFlow.Application.Common;
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
        RuleFor(x => x.Email)
            .NotEmpty()
            .Must(InputRules.IsEmail)
            .WithMessage("Enter a valid email with one @.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(InputRules.IsTenDigitPhone)
            .WithMessage("Phone number must be 10 digits.")
            .When(IsTechnician);
        RuleFor(x => x.Phone)
            .Must(InputRules.IsTenDigitPhone)
            .WithMessage("Phone number must be 10 digits.")
            .When(x => !IsTechnician(x) && !string.IsNullOrWhiteSpace(x.Phone));
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
        RuleFor(x => x.Email)
            .NotEmpty()
            .Must(InputRules.IsEmail)
            .WithMessage("Enter a valid email with one @.");
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
        RuleFor(x => x.PreferredStart)
            .NotNull()
            .WithMessage("Choose a preferred appointment date and time.");
        RuleFor(x => x.PreferredStart)
            .Must(InputRules.IsFuture)
            .WithMessage("Choose a future date and time. Past dates are not allowed.")
            .When(x => x.PreferredStart.HasValue);
        RuleFor(x => x.PreferredEnd)
            .GreaterThan(x => x.PreferredStart)
            .WithMessage("The end time must be after the start time.")
            .When(x => x.PreferredStart.HasValue && x.PreferredEnd.HasValue);
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone)
            .Must(InputRules.IsTenDigitPhone)
            .WithMessage("Phone number must be 10 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
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
        RuleFor(x => x.ArrivalStart)
            .NotNull()
            .WithMessage("Choose a proposed arrival date and time.");
        RuleFor(x => x.ArrivalStart)
            .Must(InputRules.IsFuture)
            .WithMessage("Choose a future arrival date and time. Past dates are not allowed.")
            .When(x => x.ArrivalStart.HasValue);
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
        RuleFor(x => x.Email)
            .NotEmpty()
            .Must(InputRules.IsEmail)
            .WithMessage("Enter a valid email with one @.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.ServiceArea).MaximumLength(200);
        RuleFor(x => x.Phone)
            .Must(InputRules.IsTenDigitPhone)
            .WithMessage("Phone number must be 10 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
