using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(CreateTaskDto dto, string tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskDto>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<TaskDto> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskDto dto, string tenantId, CancellationToken cancellationToken = default);
    Task<TaskDto> UpdateStatusAsync(Guid id, UpdateTaskStatusDto dto, string tenantId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
}
