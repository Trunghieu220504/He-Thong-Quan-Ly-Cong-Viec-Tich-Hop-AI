using Application.Abstractions.CQRS;
using FluentValidation;
using Projects.Application.Abstractions;
using SharedKernel;

namespace Projects.Application.Projects.GetProjects;

public sealed class GetProjectsQueryHandler(
    IValidator<GetProjectsQuery> validator,
    IProjectRepository projects)
    : IQueryHandler<GetProjectsQuery, Result<PagedResult<ProjectSummaryDto>>>
{
    public async Task<Result<PagedResult<ProjectSummaryDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<PagedResult<ProjectSummaryDto>>.ValidationFailure(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var (items, total) = await projects.GetPageByMemberAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        var dtos = items
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.Description,
                p.OwnerId,
                p.Members.First(m => m.UserId == request.UserId).Role.ToString(),
                p.Members.Count,
                p.CreatedAt))
            .ToList();

        return Result<PagedResult<ProjectSummaryDto>>.Success(
            new PagedResult<ProjectSummaryDto>(dtos, request.Page, request.PageSize, total));
    }
}