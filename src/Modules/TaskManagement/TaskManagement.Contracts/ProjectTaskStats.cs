namespace TaskManagement.Contracts;
public sealed record ProjectTaskStats(int Total, int Todo, int InProgress, int Done, int Overdue,
    IReadOnlyList<AssigneeTaskCount> ByAssignee);
public sealed record AssigneeTaskCount(Guid? AssigneeId, int Total, int Done);
