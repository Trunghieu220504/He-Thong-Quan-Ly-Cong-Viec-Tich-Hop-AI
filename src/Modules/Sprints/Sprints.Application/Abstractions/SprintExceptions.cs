namespace Sprints.Application.Abstractions;

public sealed class DuplicateActiveSprintException(Exception inner)
    : Exception("Dự án đã có sprint đang hoạt động.", inner);

public sealed class DuplicateSprintTaskException(Exception inner)
    : Exception("Task đã có trong sprint này.", inner);
