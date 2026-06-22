// Importerer Dapper — hjelper oss å kjøre SQL og konvertere resultatet til C#-objekter
using Dapper;

// Importerer SqlConnection — selve tilkoblingen til SQL Server
using Microsoft.Data.SqlClient;

// Importerer modellene våre (ToDo-klassen)
using ToDoApi;

var builder = WebApplication.CreateBuilder(args);

// Registrer tjenester for Swagger — må gjøres før builder.Build()
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bygg appen — etter dette kan vi ikke lenger registrere tjenester
var app = builder.Build();

// Aktiver Swagger kun under utvikling — ikke i produksjon
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
    app.UseSwagger();
}

// Connection string — beskriver hvor databasen er og hvordan vi kobler til
// Server=localhost       → SQL Server kjører på denne maskinen
// Database=ToDoDb        → navnet på databasen vi vil bruke
// Trusted_Connection     → bruk Windows-pålogging, ingen passord trengs
// TrustServerCertificate → stol på sertifikatet lokalt (unngår SSL-feil)
var connectionString = "Server=localhost;Database=ToDoDb;Trusted_Connection=True;TrustServerCertificate=True;";

// GET /api/todos — hent alle todos fra databasen
app.MapGet("/api/todos", async () =>
{
    // Åpne en tilkobling til databasen — lukkes automatisk når vi er ferdige (await using)
    await using var connection = new SqlConnection(connectionString);

    // Kjør SQL og konverter hver rad til et ToDo-objekt
    var todos = await connection.QueryAsync<ToDo>("SELECT * FROM Todo");

    // Returner listen med statuskode 200 OK
    return Results.Ok(todos);
});

// POST /api/todos — legg til en ny todo i databasen
app.MapPost("/api/todos", async (ToDo todo) =>
{
    // Åpne en tilkobling til databasen
    await using var connection = new SqlConnection(connectionString);

    // SQL for å sette inn en ny rad
    // @Name og @IsComplete hentes automatisk fra todo-objektet av Dapper
    // SCOPE_IDENTITY() returnerer den auto-genererte IDen fra INSERT-en
    var sql = """
              INSERT INTO Todo (Name, IsComplete)
              VALUES (@Name, @IsComplete);
              SELECT CAST(SCOPE_IDENTITY() AS INT);
              """;

    // Kjør SQL og hent den nye IDen som ble generert
    var newId = await connection.ExecuteScalarAsync<int>(sql, todo);

    // Sett den nye IDen på todo-objektet
    todo.Id = newId;

    // Returner den nye todo-en med statuskode 201 Created
    return Results.Created($"/api/todos/{newId}", todo);
});

app.Run();