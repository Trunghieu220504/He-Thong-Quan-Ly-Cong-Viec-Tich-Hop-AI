using FluentValidation;

namespace Projects.Application.Projects.GetProjects;

public sealed class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}