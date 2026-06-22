using Dapper;
using Microsoft.Data.SqlClient;
using ToDoApi;

var builder = WebApplication.CreateBuilder(args);

// Alt dette MÅ være før builder.Build()
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
    app.UseSwagger();
}

var connectionString = "Server=localhost;Database=ToDoDb;Trusted_Connection=True;TrustServerCertificate=True;";

app.MapGet("/api/todos", async ()=>
{
    await using var connection = new SqlConnection(connectionString);
    
    var todos = await connection.QueryAsync<ToDo>("SELECT * FROM Todo"); 
    return Results.Ok(todos);
});

app.MapPost("/api/todos", async (ToDo todo) =>
{
    await using var connection = new SqlConnection(connectionString);

    var sql = """
              INSERT INTO Todo (Name, IsComplete)
              VALUES (@Name, @IsComplete);
              SELECT CAST(SCOPE_IDENTITY() AS INT);
              """;
    var newId = await connection.ExecuteScalarAsync<int>(sql,todo);

    todo.Id = newId;

    return Results.Created($"/api/todos/{newId}",todo);
});

app.Run();

