using Microsoft.EntityFrameworkCore;

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
app.MapGet("/contacts", async (AppDbContext dbContext) =>
{
    var contacts = await dbContext.Contacts.ToListAsync();
    return Results.Ok(contacts);
});

// Rota para buscar um contato específico por ID
app.MapGet("/contacts/{id}", async (int id, AppDbContext dbContext) =>
{
    var contact = await dbContext.Contacts.FindAsync(id);
    if (contact == null)
    {
        return Results.NotFound("Contact not found");
    }
    return Results.Ok(contact);
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
