using Dapper;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Repositories;

public class TaskRepository
{
    private readonly IDbConnectionFactory _factory;
    public TaskRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<TaskItem> CreateAsync(int projectId, string title, DateOnly? dueDate, string metadataJson)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleAsync<TaskItem>(@"
            INSERT INTO tasks (project_id, title, due_date, metadata)
            VALUES (@ProjectId, @Title, @DueDate, @Metadata::jsonb)
            RETURNING *",
            new { ProjectId = projectId, Title = title, DueDate = dueDate, Metadata = metadataJson });
    }

    public async Task<TaskItem?> MarkDoneAsync(int taskId)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<TaskItem>(@"
            UPDATE tasks
            SET is_done = true
            WHERE id = @Id AND is_done = false
            RETURNING *", new { Id = taskId });
    }

    public async Task BulkInsertAsync(int projectId, IEnumerable<string> titles)
    {
        using var conn = _factory.Create();
        var arr = titles.ToArray();
        await conn.ExecuteAsync(@"
            INSERT INTO tasks (project_id, title)
            SELECT @ProjectId, t FROM unnest(@Titles) AS t",
            new { ProjectId = projectId, Titles = arr });
    }

    public async Task<TaskItem?> DeleteAsync(int taskId)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<TaskItem>(
            "DELETE FROM tasks WHERE id = @Id RETURNING *", new { Id = taskId });
    }
}