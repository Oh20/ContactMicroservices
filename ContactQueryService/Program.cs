using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar o DbContext para SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Rota para buscar todos os contatos
app.MapGet("/contatos", async (AppDbContext dbContext) =>
{
    var contacts = await dbContext.Contacts.ToListAsync();
    return Results.Ok(contacts);
});

// Rota para buscar um contato espec�fico por ID
app.MapGet("/contatos/{id}", async (int id, AppDbContext dbContext) =>
{
    var contact = await dbContext.Contacts.FindAsync(id);
    if (contact == null)
    {
        return Results.NotFound("Contato n�o localizado");
    }
    return Results.Ok(contact);
});

app.MapGet("/contacts/by-ddd/{ddd}", async (string ddd, AppDbContext dbContext) =>
{
    // Validar se o DDD tem 2 d�gitos e � num�rico
    if (ddd.Length != 2 || !ddd.All(char.IsDigit))
    {
        return Results.BadRequest("O DDD deve conter exatamente 2 d�gitos num�ricos.");
    }

    // Buscar contatos cujo telefone come�a com o DDD
    var contacts = await dbContext.Contacts
        .Where(c => c.Telefone.StartsWith(ddd))
        .ToListAsync();

    if (!contacts.Any())
    {
        return Results.NotFound($"Nenhum contato encontrado com o DDD {ddd}.");
    }

    return Results.Ok(contacts);
});

app.UseHttpMetrics();  // Coleta de m�tricas HTTP autom�ticas

// Exponha o endpoint /metrics
app.MapMetrics();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();