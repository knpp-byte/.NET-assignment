using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

public sealed class UpdateTaskStatusDto
{
    public TaskItemStatus Status { get; set; }
}
