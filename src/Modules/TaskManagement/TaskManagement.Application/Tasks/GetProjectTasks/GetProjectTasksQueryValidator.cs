using FluentValidation;

namespace TaskManagement.Application.Tasks.GetProjectTasks;

public sealed class GetProjectTasksQueryValidator : AbstractValidator<GetProjectTasksQuery>
{
    public GetProjectTasksQueryValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status)
            .Must(s => TaskParsing.TryParseStatus(s, out _))
            .When(x => x.Status is not null)
            .WithMessage("Trạng thái không hợp lệ. Chỉ chấp nhận: Todo, InProgress, Done.");
    }
}
