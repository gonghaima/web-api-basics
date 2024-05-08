using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;


var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddSingleton<ITaskService, InMemoryTaskService>();

builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now} Started.]");
    await next();
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now} Finished.]");

});

var todos = new List<Todo>();

// app.MapGet("/todos", () => todos);
app.MapGet("/todos", (ITaskService service) => service.GetTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) => 
{
    // var targetTodo = todos.FirstOrDefault(t => t.Id == id);
    var targetTodo = service.GetTodoById(id);
    return targetTodo is null ? TypedResults.NotFound(): TypedResults.Ok(targetTodo);
});

// Define a controller
app.MapPost("/todos/",(Todo task, ITaskService service)=>
{
    // todos.Add(task);
    service.AddTodo(task);
    return TypedResults.Created("/todos/{id}", task);
}).AddEndpointFilter(async(context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if(taskArgument.DueDate < DateTime.UtcNow)
    {
        errors.Add(nameof(Todo.DueDate), ["DueDate must be in the future"]);
    }
    
    if(taskArgument.IsCompleted)
    {
        errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);
    }

    if(errors.Count>0)
    {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
    // todos.RemoveAll(t => t.Id == id);
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();
// create an api with record
// Define a record
public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService
{
    Todo? GetTodoById(int id);
    List<Todo> GetTodos();
    void DeleteTodoById(int id);

    Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(t => t.Id == id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.FirstOrDefault(t => t.Id == id);
    }

    public List<Todo> GetTodos()
    {
        return _todos;
    }
}