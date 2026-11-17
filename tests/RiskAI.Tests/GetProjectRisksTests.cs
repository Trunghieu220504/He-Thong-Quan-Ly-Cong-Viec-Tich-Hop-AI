using RiskAI.Application.Risk;
using RiskAI.Contracts;
using SharedKernel;
using TaskManagement.Contracts;
namespace RiskAI.Tests;
public class GetProjectRisksTests
{
    [Fact]
    public async Task Batch_includes_only_project_tasks_and_uses_actual_features()
    {
        var project = Guid.NewGuid(); var user = Guid.NewGuid();
        var tasks = new FakeTaskModuleApi(); var projects = new FakeProjectsModuleApi();
        projects.Roles[(project, user)] = "Viewer";
        var a = new TaskSummary(Guid.NewGuid(), project, ProjectTaskStatus.InProgress, null, null, 4, 23);
        tasks.Tasks[a.Id] = a;
        var other = a with { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() }; tasks.Tasks[other.Id] = other;
        var ai = new FakeRiskAiClient();
        var handler = new GetProjectRisksQueryHandler(projects,tasks,ai,TimeProvider.System);
        var result = await handler.Handle(new(project,user),default);
        Assert.Equal(a.Id, Assert.Single(result.Value!.Items).TaskId);
        Assert.Equal(new RiskAiRequest(a.Id,4,null,23),Assert.Single(ai.Calls));
        tasks.Tasks.Clear(); ai.Calls.Clear();
        result = await handler.Handle(new(project,user),default);
        Assert.Empty(result.Value!.Items); Assert.Empty(ai.Calls);
        var forbidden = await handler.Handle(new(project,Guid.NewGuid()),default);
        Assert.Equal(ErrorType.Forbidden,forbidden.ErrorType); Assert.Empty(ai.Calls);
    }
}
