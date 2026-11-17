namespace TaskBoard.Api.Models;

public enum TaskItemStatus
{
    Todo,
    InProgress,
    Done
}

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public string? Owner { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Project? Project { get; set; }
}
