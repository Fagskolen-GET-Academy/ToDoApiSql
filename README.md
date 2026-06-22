# Forelesning: Todo API med Dapper og SQL Server 2025 Developer

## Læringsmål

Etter denne forelesningen skal studentene kunne:

- Sette opp et ASP.NET Core Minimal API-prosjekt
- Koble til SQL Server 2025 Developer med Dapper
- Lage en tabell manuelt med SQL i Rider
- Lage `GET` og `POST` endepunkter
- Teste API-et med Swagger

---

## Ordliste

| Begrep | Forklaring |
|---|---|
| **API** | Et grensesnitt som lar programmer snakke med hverandre over HTTP |
| **Endpoint** | En URL i API-et som svarer på en bestemt type request (GET, POST osv.) |
| **Dapper** | Et lite bibliotek som hjelper deg å kjøre SQL og konvertere resultatet til C#-objekter |
| **SQL Server 2025 Developer** | En gratis fullversjonen av SQL Server for utvikling — ingen begrensninger på størrelse eller funksjoner |
| **Connection string** | En tekststreng som beskriver hvor databasen er og hvordan man logger inn |
| **SQL** | Språket man bruker til å snakke med en database (hente, lagre, slette data) |
| **NuGet** | Pakkebehandler for C# — samme konsept som `npm` i JavaScript |
| **Swagger** | Et visuelt verktøy for å teste API-et direkte i nettleseren |
| **`using`** | Sørger for at en ressurs (f.eks. databasetilkobling) lukkes automatisk når den ikke lenger er i bruk |

---

## Forutsetninger

- Rider installert
- SQL Server 2025 Developer installert — last ned gratis fra [microsoft.com/sql-server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

---

## 1. Lag et nytt prosjekt

I Rider: **New Solution → ASP.NET Core Web Application → Empty**

Kall prosjektet `TodoApi`.

---

## 2. Installer NuGet-pakker

Åpne terminalen i Rider og kjør:

```bash
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package Swashbuckle.AspNetCore
```

- **Dapper** — lar oss skrive SQL og få resultatet tilbake som C#-objekter
- **Microsoft.Data.SqlClient** — selve driveren som kobler C# til SQL Server / SQL Server 2025 Developer
- **Swashbuckle.AspNetCore** — gir oss Swagger UI for å teste API-et i nettleseren

---

## 3. Koble til SQL Server 2025 Developer i Rider

1. **View → Tool Windows → Database**
2. Klikk **+** → **Data Source** → **Microsoft SQL Server**
3. Velg **Use connection string** og skriv inn:

```
Server=localhost;Database=master;Trusted_Connection=True;
```

4. Klikk **Test Connection** → godta driver-nedlasting hvis Rider spør → **OK**

> **`localhost`** er standardinstansnavnet etter installasjon av SQL Server 2025 Developer.
> **`Trusted_Connection=True`** betyr at vi bruker Windows-påloggingen vår — ingen passord trengs.

---

## 4. Opprett database og tabell

Høyreklikk på datasourcen → **New → Query Console** og kjør:

```sql
CREATE DATABASE TodoDb;
GO

USE TodoDb;

CREATE TABLE Todo (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(MAX),
    IsComplete BIT NOT NULL DEFAULT 0
);
```

Linje for linje:

| SQL | Forklaring |
|---|---|
| `CREATE DATABASE TodoDb` | Opprett en ny database kalt `TodoDb` |
| `GO` | Kjør forrige kommando før du fortsetter |
| `USE TodoDb` | Si at de neste kommandoene skal kjøres mot `TodoDb` |
| `Id INT IDENTITY PRIMARY KEY` | Heltall som settes automatisk — du trenger aldri sende inn `Id` selv |
| `Name NVARCHAR(MAX)` | Tekst — kan være tom (nullable) |
| `IsComplete BIT NOT NULL DEFAULT 0` | Boolean — `0` = false, `1` = true. Standard er `0` (ikke fullført) |

Refresh datasourcen — du skal se `TodoDb` med `Todo`-tabellen under **schemas → dbo → tables**.

---

## 5. Modellen

Lag en ny fil `Todo.cs`. Modellen representerer én rad i `Todo`-tabellen:

```csharp
// Todo.cs
namespace TodoApi;

public class Todo
{
    public int Id { get; set; }       // matcher Id-kolonnen i databasen
    public string? Name { get; set; } // matcher Name-kolonnen
    public bool IsComplete { get; set; } // matcher IsComplete-kolonnen
}
```

> Dapper mapper kolonnenavnene fra SQL direkte til egenskapene i klassen.
> Derfor er det viktig at navnene matcher — `Name` i C# = `Name` i SQL.

---

## 6. Program.cs

```csharp
using Dapper;
using Microsoft.Data.SqlClient;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// Legg til tjenester for Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Vis Swagger kun under utvikling — ikke i produksjon
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Connection string — beskriver hvor databasen er og hvordan vi kobler til
var connectionString = "Server=localhost;Database=TodoDb;Trusted_Connection=True;";

// GET /api/todos — hent alle todos
app.MapGet("/api/todos", async () =>
{
    // Åpne en tilkobling til databasen
    using var connection = new SqlConnection(connectionString);

    // Kjør SQL og få resultatet tilbake som en liste av Todo-objekter
    var todos = await connection.QueryAsync<Todo>("SELECT * FROM Todo");

    // Returner listen med statuskode 200 OK
    return Results.Ok(todos);
});

// POST /api/todos — legg til en ny todo
app.MapPost("/api/todos", async (Todo todo) =>
{
    // Åpne en tilkobling til databasen
    using var connection = new SqlConnection(connectionString);

    var sql = """
        INSERT INTO Todo (Name, IsComplete)
        VALUES (@Name, @IsComplete);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
        """;

    // Kjør SQL — @Name og @IsComplete hentes automatisk fra todo-objektet
    // QuerySingleAsync<int> returnerer den nye IDen som ble generert
    var newId = await connection.QuerySingleAsync<int>(sql, todo);

    // Sett den nye IDen på todo-objektet
    todo.Id = newId;

    // Returner den nye todo-en med statuskode 201 Created
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.Run();
```

---

## 8. Test i Swagger

Kjør appen og åpne `https://localhost:{port}/swagger`.

**Steg 1 — legg til en todo:**

Klikk på **POST /api/todos** → **Try it out** → lim inn:

```json
{
  "name": "Lær Dapper",
  "isComplete": false
}
```

Klikk **Execute**. Du skal få statuskode `201 Created` tilbake.

**Steg 2 — hent alle todos:**

Klikk på **GET /api/todos** → **Try it out** → **Execute**.

Du skal se todo-en du nettopp la til.

**Steg 3 — åpne databasen i Rider:**

Refresh `Todo`-tabellen i Database-vinduet. Du skal se raden som ble lagret.

---

## Flyten — hva skjer egentlig?

```
Swagger sender POST /api/todos med JSON-body
        ↓
ASP.NET Core mottar requesten og parser JSON til et Todo-objekt
        ↓
app.MapPost kjøres
        ↓
Dapper åpner en tilkobling til SQL Server 2025 Developer
        ↓
SQL INSERT kjøres mot Todo-tabellen
        ↓
Den nye IDen returneres og settes på todo-objektet
        ↓
Results.Created(...) sendes tilbake som HTTP-respons
```

---

> **Tips:** Åpne `Todo`-tabellen i Rider ved siden av Swagger mens du POSTer.
