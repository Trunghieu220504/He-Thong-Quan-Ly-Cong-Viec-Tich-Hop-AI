using FluentValidation;

namespace TaskManagement.Application.Tasks.AssignTask;

public sealed class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.AssigneeId).NotEqual(Guid.Empty).When(x => x.AssigneeId.HasValue);
    }
}
