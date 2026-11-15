using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Tasks.UpdateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
namespace Dashboard.Tests;
public class UpdateTaskTests
{
    [Theory]
    [InlineData("Viewer", ErrorType.Forbidden, false)]
    [InlineData(null, ErrorType.Forbidden, false)]
    [InlineData("Member", ErrorType.None, true)]
    public async Task Updating_features_enforces_project_write_access(string? role, ErrorType expected, bool saved)
    {
        var data = new Store(role);
        var result = await new UpdateTaskCommandHandler(data, data, data, data).Handle(
            new(data.Row.Id, Guid.NewGuid(), "Updated", "Details", "High", null, 18.5, 64), default);
        Assert.Equal(expected, result.ErrorType);
        Assert.Equal(saved, data.Saved);
        Assert.Equal(saved ? 64 : 0, data.Row.ProgressPercent);
    }
    private sealed class Store(string? role) : ITaskRepository, ITaskManagementUnitOfWork, IProjectsModuleApi, IIdentityModuleApi
    {
        public ProjectTask Row { get; } = ProjectTask.Create(Guid.NewGuid(), "Task", null, TaskPriority.Low, null, null, Guid.NewGuid()).Value!;
        public bool Saved { get; private set; }
        public Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ProjectTask?>(Row);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) { Saved = true; return Task.FromResult(1); }
        public Task<string?> GetMemberRoleAsync(Guid id, Guid user, CancellationToken ct = default) => Task.FromResult(role);
        public Task<bool> ProjectExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UserExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<UserSummary?> GetUserSummaryAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UserSummary?>(null);
        public void Add(ProjectTask task) => throw new NotSupportedException();
        public Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPageByProjectAsync(Guid projectId, ProjectTaskStatus? status, int page, int pageSize, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
