using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.Dtos;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class TasksController(AppDbContext db) : ControllerBase
{
    /// Lists a project's tasks, with optional filters
    [HttpGet("projects/{projectId:int}/tasks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TaskResponse>>> GetForProject(
        int projectId, [FromQuery] TaskItemStatus? status, [FromQuery] string? owner)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId)) return NotFound();

        var query = db.Tasks.Where(t => t.ProjectId == projectId);
        if (status is not null) query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(owner))
        {
            var wanted = owner.Trim().ToLower();
            query = query.Where(t => t.Owner != null && t.Owner.ToLower() == wanted);
        }

        var tasks = await query.OrderBy(t => t.Id).ToListAsync();
        return tasks.Select(ToResponse).ToList();
    }

    // Add a task to a project
    [HttpPost("projects/{projectId:int}/tasks")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Create(int projectId, TaskRequest request)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId)) return NotFound();

        var task = new TaskItem { ProjectId = projectId, CreatedAt = DateTime.UtcNow };
        Apply(request, task);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToResponse(task));
    }

    /// Get one task by id
    [HttpGet("tasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        return task is null ? NotFound() : ToResponse(task);
    }

    /// Replaces all of a task's details
    [HttpPut("tasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(int id, TaskRequest request)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        Apply(request, task);
        await db.SaveChangesAsync();
        return ToResponse(task);
    }

    /// Move a task to another column without touching its other details
    [HttpPatch("tasks/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(int id, TaskStatusRequest request)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        task.Status = request.Status!.Value;
        await db.SaveChangesAsync();
        return ToResponse(task);
    }

    ///  Delete a task
    [HttpDelete("tasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(TaskRequest request, TaskItem task)
    {
        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = request.Status;
        task.Owner = string.IsNullOrWhiteSpace(request.Owner) ? null : request.Owner.Trim();
        task.DueDate = request.DueDate;
    }

    private static TaskResponse ToResponse(TaskItem t) =>
        new(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Owner, t.DueDate, t.CreatedAt);
}
