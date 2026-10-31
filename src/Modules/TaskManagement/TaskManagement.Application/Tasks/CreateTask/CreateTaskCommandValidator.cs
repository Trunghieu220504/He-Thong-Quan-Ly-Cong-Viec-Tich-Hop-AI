using FluentValidation;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Tasks.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(ProjectTask.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(ProjectTask.DescriptionMaxLength);
        RuleFor(x => x.Priority)
            .Must(p => TaskParsing.TryParsePriority(p, out _))
            .WithMessage("Mức ưu tiên không hợp lệ. Chỉ chấp nhận: Low, Medium, High.");
        RuleFor(x => x.AssigneeId).NotEqual(Guid.Empty).When(x => x.AssigneeId.HasValue);
    }
}
