using FluentValidation;

namespace Sprints.Application.Sprints.CreateSprint;

public sealed class CreateSprintCommandValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintCommandValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.EndDate)
            .GreaterThan(command => command.StartDate)
            .When(command => command.StartDate.HasValue && command.EndDate.HasValue);
    }
}
