using FixFlow.Application.DTOs.Agents;
using FluentValidation;

namespace FixFlow.Application.Validators;

public class AgentRequestValidator : AbstractValidator<AgentRequest>
{
    public AgentRequestValidator()
    {
        RuleFor(x => x.Goal).NotEmpty().MaximumLength(500);
    }
}
