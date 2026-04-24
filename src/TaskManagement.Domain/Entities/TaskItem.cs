using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

public sealed class TaskItem
{
    private TaskItem()
    {
        Title = string.Empty;
        Description = string.Empty;
        AssignedTo = string.Empty;
        TenantId = string.Empty;
    }

    public TaskItem(
        string title,
        string description,
        TaskPriority priority,
        DateTime? dueDate,
        string assignedTo,
        string tenantId)
    {
        Id = Guid.NewGuid();
        Title = NormalizeRequired(title, nameof(title));
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        DueDate = dueDate;
        AssignedTo = assignedTo?.Trim() ?? string.Empty;
        TenantId = NormalizeRequired(tenantId, nameof(tenantId));
        Status = TaskItemStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string AssignedTo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public string TenantId { get; private set; }

    public void Update(
        string title,
        string description,
        TaskPriority priority,
        DateTime? dueDate,
        string assignedTo)
    {
        EnsureEditable();

        Title = NormalizeRequired(title, nameof(title));
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        DueDate = dueDate;
        AssignedTo = assignedTo?.Trim() ?? string.Empty;
        Touch();
    }

    public void UpdateStatus(TaskItemStatus newStatus)
    {
        if (Status == TaskItemStatus.Completed && newStatus == TaskItemStatus.InProgress)
        {
            throw new DomainException("Completed tasks cannot be moved back to in progress.");
        }

        if (Status == TaskItemStatus.Cancelled)
        {
            throw new DomainException("Cancelled tasks are read-only.");
        }

        Status = newStatus;
        Touch();
    }

    public void MarkDeleted()
    {
        if (Status == TaskItemStatus.Completed)
        {
            throw new DomainException("Completed tasks cannot be deleted.");
        }

        if (Status == TaskItemStatus.Cancelled)
        {
            throw new DomainException("Cancelled tasks are read-only.");
        }

        IsDeleted = true;
        Touch();
    }

    private void EnsureEditable()
    {
        if (Status == TaskItemStatus.Cancelled)
        {
            throw new DomainException("Cancelled tasks are read-only.");
        }
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{parameterName} is required.");
        }

        return value.Trim();
    }
}
