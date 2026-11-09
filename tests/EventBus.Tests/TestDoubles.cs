using Microsoft.Extensions.Logging;
using SharedKernel;

namespace EventBus.Tests;

public sealed record SampleEvent(Guid EventId, string Name) : IIntegrationEvent;

public sealed record OtherEvent(Guid EventId) : IIntegrationEvent;

/// <summary>Handler ghi lại thứ tự được gọi vào một danh sách dùng chung.</summary>
public sealed class RecordingHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    private readonly string _name;
    private readonly List<string> _calls;

    public RecordingHandler(string name, List<string> calls)
    {
        _name = name;
        _calls = calls;
    }

    public Task Handle(TEvent @event, CancellationToken cancellationToken)
    {
        _calls.Add(_name);
        return Task.CompletedTask;
    }
}

/// <summary>Handler luôn ném lỗi, dùng để kiểm tra bus cô lập lỗi.</summary>
public sealed class ThrowingHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    private readonly Exception _exception;
    private readonly List<string> _calls;

    public ThrowingHandler(Exception exception, List<string> calls)
    {
        _exception = exception;
        _calls = calls;
    }

    public Task Handle(TEvent @event, CancellationToken cancellationToken)
    {
        _calls.Add("throwing");
        throw _exception;
    }
}

public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

public sealed class ListLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
}
