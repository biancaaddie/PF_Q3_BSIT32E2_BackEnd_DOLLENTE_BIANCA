using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        {
            return BadRequest("Task cannot be empty.");
        }

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Todos (Task, IsCompleted)
            VALUES (@task, @isCompleted);
            SELECT last_insert_rowid();
        ";

        var taskParam = command.CreateParameter();
        taskParam.ParameterName = "@task";
        taskParam.Value = item.Task;
        command.Parameters.Add(taskParam);

        var completedParam = command.CreateParameter();
        completedParam.ParameterName = "@isCompleted";
        completedParam.Value = item.IsCompleted ? 1 : 0;
        command.Parameters.Add(completedParam);

        var result = await command.ExecuteScalarAsync();
        item.Id = Convert.ToInt32(result);

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
            item.Task,
            item.IsCompleted ? 1 : 0,
            item.Id);

        if (rowsAffected == 0)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM Todos WHERE Id = {0}",
            id);

        if (rowsAffected == 0)
        {
            return NotFound();
        }

        return NoContent();
    }
}