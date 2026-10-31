using FluentValidation;

namespace TaskManagement.Application.Tasks.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => TaskParsing.TryParseStatus(s, out _))
            .WithMessage("Trạng thái không hợp lệ. Chỉ chấp nhận: Todo, InProgress, Done.");
    }
}
