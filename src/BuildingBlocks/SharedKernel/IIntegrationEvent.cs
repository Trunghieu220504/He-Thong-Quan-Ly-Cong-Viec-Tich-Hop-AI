namespace SharedKernel;

/// <summary>
/// Marker cho sự kiện tích hợp giữa các module. Nằm ở SharedKernel (không phải EventBus) để project
/// Contracts của từng module khai báo được sự kiện mà không phải phụ thuộc EventBus
/// (ArchitectureTests cấm Contracts tham chiếu EventBus).
/// </summary>
public interface IIntegrationEvent;
