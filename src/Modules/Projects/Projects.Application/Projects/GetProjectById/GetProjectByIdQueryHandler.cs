using Application.Abstractions.CQRS;
using Identity.Contracts;
using Projects.Application.Abstractions;
using SharedKernel;

namespace Projects.Application.Projects.GetProjectById;

public sealed class GetProjectByIdQueryHandler(IProjectRepository projects, IIdentityModuleApi identity)
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

        var memberDtos = new List<ProjectMemberDto>();
        foreach (var m in project.Members.OrderBy(x => x.JoinedAt))
        {
            var userSummary = await identity.GetUserSummaryAsync(m.UserId, cancellationToken);
            memberDtos.Add(new ProjectMemberDto(
                m.UserId,
                userSummary?.DisplayName,
                m.Role.ToString(),
                m.JoinedAt));
        }

        return Result<ProjectDetailDto>.Success(new ProjectDetailDto(
            project.Id, project.Name, project.Description, project.OwnerId,
            me.Role.ToString(), project.CreatedAt, project.UpdatedAt, memberDtos));
    }
}