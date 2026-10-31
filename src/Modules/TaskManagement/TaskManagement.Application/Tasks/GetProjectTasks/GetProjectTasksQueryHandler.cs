using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks.GetProjectTasks;

public sealed class GetProjectTasksQueryHandler(
    IValidator<GetProjectTasksQuery> validator,
    IProjectsModuleApi projects,
    IIdentityModuleApi identity,
    ITaskRepository tasks)
    : IQueryHandler<GetProjectTasksQuery, Result<PagedResult<TaskDto>>>
{
    public async Task<Result<PagedResult<TaskDto>>> Handle(GetProjectTasksQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<PagedResult<TaskDto>>.ValidationFailure(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var access = await ProjectAccess.RequireAsync(projects, request.ProjectId, request.UserId, write: false, cancellationToken);
        if (access.IsFailure)
            return ProjectAccess.ToFailure<PagedResult<TaskDto>>(access);

        ProjectTaskStatus? status = null;
        if (request.Status is not null && TaskParsing.TryParseStatus(request.Status, out var parsed))
            status = parsed;

        var (items, total) = await tasks.GetPageByProjectAsync(
            request.ProjectId, status, request.Page, request.PageSize, cancellationToken);

        // Mỗi người được giao chỉ tra tên một lần cho cả trang.
        var names = new Dictionary<Guid, string?>();
        foreach (var assigneeId in items.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value).Distinct())
            names[assigneeId] = await TaskDtoMapper.GetDisplayNameAsync(identity, assigneeId, cancellationToken);

        var dtos = items
            .Select(t => TaskDtoMapper.ToDto(t, t.AssigneeId is { } id ? names[id] : null))
            .ToList();

        return Result<PagedResult<TaskDto>>.Success(
            new PagedResult<TaskDto>(dtos, request.Page, request.PageSize, total));
    }
}
