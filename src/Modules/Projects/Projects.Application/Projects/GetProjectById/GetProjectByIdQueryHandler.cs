using Application.Abstractions.CQRS;
using Projects.Application.Abstractions;
using SharedKernel;

namespace Projects.Application.Projects.GetProjectById;

public sealed class GetProjectByIdQueryHandler(IProjectRepository projects)
    : IQueryHandler<GetProjectByIdQuery, Result<ProjectDetailDto>>
{
    public async Task<Result<ProjectDetailDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Result<ProjectDetailDto>.NotFound("Không tìm thấy dự án.");

        var me = project.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (me is null)
            return Result<ProjectDetailDto>.Forbidden("Bạn không phải thành viên của dự án này.");

        var members = project.Members
            .OrderBy(m => m.JoinedAt)
            .Select(m => new ProjectMemberDto(m.UserId, m.Role.ToString(), m.JoinedAt))
            .ToList();

        return Result<ProjectDetailDto>.Success(new ProjectDetailDto(
            project.Id, project.Name, project.Description, project.OwnerId,
            me.Role.ToString(), project.CreatedAt, project.UpdatedAt, members));
    }
}