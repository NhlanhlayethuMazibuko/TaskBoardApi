using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.Dtos;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Produces("application/json")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists all projects, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectResponse>>> GetAll()
    {
        return await db.Projects
            .OrderByDescending(p => p.Id)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Description, p.CreatedAt, p.Tasks.Count))
            .ToListAsync();
    }

    /// <summary>Get one project by id</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        var project = await db.Projects
            .Where(p => p.Id == id)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Description, p.CreatedAt, p.Tasks.Count))
            .FirstOrDefaultAsync();

        return project is null ? NotFound() : project;
    }

    /// <summary>Create a project</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var response = new ProjectResponse(project.Id, project.Name, project.Description, project.CreatedAt, 0);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, response);
    }

    /// <summary>Edit a project's name and description</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> Update(int id, ProjectRequest request)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound();

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        await db.SaveChangesAsync();

        var taskCount = await db.Tasks.CountAsync(t => t.ProjectId == id);
        return new ProjectResponse(project.Id, project.Name, project.Description, project.CreatedAt, taskCount);
    }

    /// <summary>Delete a project and all of its tasks</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound();

        db.Projects.Remove(project);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
