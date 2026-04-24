using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Tests;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task CreateAsync_Throws_WhenHighPriorityTaskHasNoDueDate()
    {
        var service = new TaskService(new InMemoryTaskRepository());

        var request = new CreateTaskDto
        {
            Title = "Prepare incident report",
            Priority = TaskPriority.High
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(request, "tenant-a"));
    }

    [Fact]
    public async Task CreateAsync_AssignsTenantAndDefaultsStatus()
    {
        var repository = new InMemoryTaskRepository();
        var service = new TaskService(repository);

        var result = await service.CreateAsync(new CreateTaskDto
        {
            Title = "Review backlog",
            Description = "Prioritize work for the next sprint",
            Priority = TaskPriority.Medium,
            AssignedTo = "delivery-lead"
        }, "tenant-a");

        Assert.Equal(TaskItemStatus.Pending, result.Status);
        Assert.Single(repository.Items);
        Assert.Equal("tenant-a", repository.Items[0].TenantId);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenTaskIsCompleted()
    {
        var repository = new InMemoryTaskRepository();
        var task = new TaskItem("Release checklist", string.Empty, TaskPriority.Medium, null, string.Empty, "tenant-a");
        task.UpdateStatus(TaskItemStatus.Completed);
        repository.Items.Add(task);

        var service = new TaskService(repository);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.DeleteAsync(task.Id, "tenant-a"));
    }

    [Fact]
    public async Task UpdateStatusAsync_Throws_WhenCompletedTaskMovesBackToInProgress()
    {
        var repository = new InMemoryTaskRepository();
        var task = new TaskItem("Close audit finding", string.Empty, TaskPriority.Medium, null, string.Empty, "tenant-a");
        task.UpdateStatus(TaskItemStatus.Completed);
        repository.Items.Add(task);

        var service = new TaskService(repository);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateStatusAsync(task.Id, new UpdateTaskStatusDto
            {
                Status = TaskItemStatus.InProgress
            }, "tenant-a"));
    }

    private sealed class InMemoryTaskRepository : ITaskRepository
    {
        public List<TaskItem> Items { get; } = [];

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            Items.Add(task);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaskItem>>(Items.Where(x => !x.IsDeleted).ToArray());
        }

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
