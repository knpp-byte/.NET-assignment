using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto, string tenantId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(tenantId);
        EnsurePriorityRules(dto.Priority, dto.DueDate);

        var task = new TaskItem(dto.Title, dto.Description, dto.Priority, dto.DueDate, dto.AssignedTo, tenantId);

        await _repository.AddAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return TaskDto.FromEntity(task);
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(tenantId);

        var tasks = await _repository.GetAllAsync(cancellationToken);
        return tasks.Select(TaskDto.FromEntity).ToArray();
    }

    public async Task<TaskDto> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(tenantId);

        var task = await GetExistingTaskAsync(id, cancellationToken);
        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskDto dto, string tenantId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(tenantId);
        EnsurePriorityRules(dto.Priority, dto.DueDate);

        var task = await GetExistingTaskAsync(id, cancellationToken);
        task.Update(dto.Title, dto.Description, dto.Priority, dto.DueDate, dto.AssignedTo);

        await _repository.SaveChangesAsync(cancellationToken);
        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> UpdateStatusAsync(Guid id, UpdateTaskStatusDto dto, string tenantId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(tenantId);

        var task = await GetExistingTaskAsync(id, cancellationToken);
        task.UpdateStatus(dto.Status);

        await _repository.SaveChangesAsync(cancellationToken);
        return TaskDto.FromEntity(task);
    }

    public async Task DeleteAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(tenantId);

        var task = await GetExistingTaskAsync(id, cancellationToken);
        task.MarkDeleted();

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> GetExistingTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(id, cancellationToken);
        return task ?? throw new NotFoundException($"Task '{id}' was not found.");
    }

    private static void EnsurePriorityRules(TaskPriority priority, DateTime? dueDate)
    {
        if (priority == TaskPriority.High && dueDate is null)
        {
            throw new DomainException("High priority tasks must have a due date.");
        }
    }

    private static void EnsureTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new DomainException("Tenant id is required.");
        }
    }
}
