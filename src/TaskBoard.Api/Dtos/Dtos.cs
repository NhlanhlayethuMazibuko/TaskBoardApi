using System.ComponentModel.DataAnnotations;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Dtos;

public class ProjectRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }
}

public record ProjectResponse(int Id, string Name, string? Description, DateTime CreatedAt, int TaskCount);

public class TaskRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    [StringLength(100)]
    public string? Owner { get; set; }

    public DateOnly? DueDate { get; set; }
}

public class TaskStatusRequest
{
    [Required]
    public TaskItemStatus? Status { get; set; }
}

public record TaskResponse(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    string? Owner,
    DateOnly? DueDate,
    DateTime CreatedAt);
