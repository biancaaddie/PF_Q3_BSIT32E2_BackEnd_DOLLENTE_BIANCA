using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using TodoApi.Data;
using TodoApi.Models;
namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _context;
    public TodoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodos()
    {
        var todos = await _context.Database
        .SqlQueryRaw<TodoItem>("SELECT Id, Task, IsCompleted FROM Todos")
        .ToListAsync();
        return Ok(todos);
    }
    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateTodoItem([FromBody] TodoItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Task))
            return BadRequest("Task cannot be empty.");

        await _context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Todos (Task, IsCompleted) VALUES ({0}, {1})",
            item.Task,
            item.IsCompleted ? 1 : 0
        );

        var newId = await _context.Database
            .SqlQueryRaw<int>("SELECT last_insert_rowid() AS Value")
            .SingleAsync();

        item.Id = newId;

        return Ok(item);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodoItem(int id, [FromBody] TodoItem item)
    {
        if (id != item.Id)
        {
            return BadRequest("ID mismatch.");
        }
        int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE Todos SET Task = {0}, IsCompleted = {1} WHERE Id = {2}",
            item.Task, item.IsCompleted ? 1 : 0, item.Id);
        if (rowsAffected == 0)
        {
            return NotFound();
        }
        return NoContent();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM Todos WHERE Id = {0}", id);
        if (rowsAffected == 0)
        {
            return NotFound();
        }
        return NoContent();
    }
}