using Application.Abstractions.CQRS;
using Projects.Contracts;
using SharedKernel;
using Sprints.Application.Abstractions;

namespace Sprints.Application.Sprints.GetProjectSprints;

public sealed class GetProjectSprintsQueryHandler(IProjectsModuleApi projects, ISprintRepository sprints)
    : IQueryHandler<GetProjectSprintsQuery, Result<IReadOnlyList<SprintDto>>>
{
    public async Task<Result<IReadOnlyList<SprintDto>>> Handle(
        GetProjectSprintsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await projects.ProjectExistsAsync(request.ProjectId, cancellationToken))
            return Result<IReadOnlyList<SprintDto>>.NotFound("Không tìm thấy dự án.");

        var role = await projects.GetMemberRoleAsync(request.ProjectId, request.ActorId, cancellationToken);
        if (role is null)
            return Result<IReadOnlyList<SprintDto>>.Forbidden("Bạn không phải thành viên của dự án này.");

        var projectSprints = await sprints.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return Result<IReadOnlyList<SprintDto>>.Success(projectSprints.Select(sprint => sprint.ToDto()).ToList());
    }
}
