using System;
using System.Collections.Generic;

namespace TaskTracker.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerId { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Settings { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public List<TaskItem> Tasks { get; set; } = new();
}

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Metadata { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}