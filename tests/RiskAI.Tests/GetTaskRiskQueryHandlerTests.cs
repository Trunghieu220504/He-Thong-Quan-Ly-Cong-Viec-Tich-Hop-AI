using System.Globalization;
using RiskAI.Application.Risk;
using RiskAI.Application.Risk.GetTaskRisk;
using RiskAI.Contracts;
using SharedKernel;
using TaskManagement.Contracts;

namespace RiskAI.Tests;

public class GetTaskRiskQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 0, 0, TimeSpan.Zero);

    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeTaskModuleApi _tasks = new();
    private readonly FakeProjectsModuleApi _projects = new();
    private readonly FakeRiskAiClient _ai = new();

    private Task<Result<TaskRiskDto>> Send(Guid taskId) =>
        new GetTaskRiskQueryHandler(_tasks, _projects, _ai, new FixedTimeProvider(Now))
            .Handle(new GetTaskRiskQuery(taskId, _userId), CancellationToken.None);

    private TaskSummary AddTask(
        ProjectTaskStatus status = ProjectTaskStatus.InProgress,
        DateTime? dueDate = null,
        bool userIsMember = true)
    {
        var task = new TaskSummary(Guid.NewGuid(), _projectId, status, dueDate, AssigneeId: null);
        _tasks.Tasks[task.Id] = task;
        if (userIsMember)
            _projects.Roles[(_projectId, _userId)] = "Member";

        return task;
    }

    private static DateTime Utc(string iso) =>
        DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    [Fact]
    public async Task Task_khong_ton_tai_tra_NotFound_va_khong_goi_AI()
    {
        var result = await Send(Guid.NewGuid());

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(_ai.Calls);
    }

    [Fact]
    public async Task Khong_phai_thanh_vien_du_an_tra_Forbidden_va_khong_goi_AI()
    {
        var task = AddTask(userIsMember: false);
        _projects.Roles[(_projectId, Guid.NewGuid())] = "Owner"; // dự án có thành viên khác, nhưng không có user này

        var result = await Send(task.Id);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Empty(_ai.Calls);
    }

    [Fact]
    public async Task Thanh_vien_nhan_risk_tu_ai_service()
    {
        var task = AddTask(ProjectTaskStatus.InProgress, Utc("2026-10-27T08:00:00Z"));
        _ai.Respond = request => new RiskAiResult(request.TaskId, "medium");

        var result = await Send(task.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TaskRiskDto(task.Id, "medium", "ai-service"), result.Value);
        var sent = Assert.Single(_ai.Calls);
        Assert.Equal(new RiskAiRequest(task.Id, EstimatedHours: null, DaysUntilDue: 3, ProgressPercent: 50), sent);
    }

    [Theory]
    [InlineData("2026-10-27T08:00:00Z", 3)]  // còn đúng 3 ngày
    [InlineData("2026-10-27T20:00:00Z", 3)]  // còn 3,5 ngày → làm tròn xuống
    [InlineData("2026-10-24T20:00:00Z", 0)]  // hạn ngay trong hôm nay, còn 12 giờ
    [InlineData("2026-10-24T07:00:00Z", -1)] // vừa quá hạn 1 giờ
    [InlineData("2026-10-21T08:00:00Z", -3)] // quá hạn 3 ngày
    public async Task Days_until_due_tinh_tu_DueDate(string dueDate, int expectedDays)
    {
        var task = AddTask(dueDate: Utc(dueDate));

        await Send(task.Id);

        Assert.Equal<int?>(expectedDays, Assert.Single(_ai.Calls).DaysUntilDue);
    }

    [Fact]
    public async Task Task_khong_co_DueDate_thi_days_until_due_null()
    {
        var task = AddTask(dueDate: null);

        await Send(task.Id);

        Assert.Null(Assert.Single(_ai.Calls).DaysUntilDue);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Todo, 0d)]
    [InlineData(ProjectTaskStatus.InProgress, 50d)]
    [InlineData(ProjectTaskStatus.Done, 100d)]
    public async Task Progress_percent_suy_tu_trang_thai(ProjectTaskStatus status, double expectedProgress)
    {
        var task = AddTask(status);

        await Send(task.Id);

        Assert.Equal<double?>(expectedProgress, Assert.Single(_ai.Calls).ProgressPercent);
    }

    [Fact]
    public async Task Ai_service_loi_thi_tra_Failure_kem_thong_bao()
    {
        var task = AddTask();
        _ai.Respond = _ => throw new RiskAiUnavailableException("AI service quá thời gian phản hồi.");

        var result = await Send(task.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("AI service quá thời gian phản hồi.", result.Error);
    }
}
