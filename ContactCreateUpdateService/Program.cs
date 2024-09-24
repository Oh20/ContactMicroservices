using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.ComponentModel.DataAnnotations;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/contatos", async ([FromBody] ContactDto contact, HttpContext httpContext) =>
{
    // Valida��o de modelo
    var validationContext = new ValidationContext(contact, null, null);
    var validationResults = new List<ValidationResult>();

    if (!Validator.TryValidateObject(contact, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { Errors = errors });
    }

    // Configura��o do RabbitMQ
    var factory = new ConnectionFactory() { HostName = "localhost" };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // Declara��o da fila
    channel.QueueDeclare(queue: "contact_queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    // Serializa��o do objeto de contato para JSON
    var message = System.Text.Json.JsonSerializer.Serialize(contact);
    var body = Encoding.UTF8.GetBytes(message);

    // Publica a mensagem na fila
    channel.BasicPublish(exchange: "",
                         routingKey: "contact_queue",
                         basicProperties: null,
                         body: body);

    return Results.Ok("Contato direcionado � fila de Cria��o");
});

app.MapPut("/contacts/{name}", ([FromRoute] string name, [FromBody] ContactDto contact) =>
{
    // Valida��o dos campos do contato
    if (string.IsNullOrWhiteSpace(contact.Nome) || string.IsNullOrWhiteSpace(contact.Telefone) || string.IsNullOrWhiteSpace(contact.Email))
    {
        return Results.BadRequest("Nome, Telefone ou Email n�o podem estar vazios.");
    }

    // Atualiza o nome do contato para garantir consist�ncia
    contact.Nome = name;

    // Configura��o do RabbitMQ
    var factory = new ConnectionFactory() { HostName = "localhost" }; // Substitua pelo hostname do RabbitMQ
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // Declara��o da fila de atualiza��o
    channel.QueueDeclare(queue: "contact_update_queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    // Serializa��o do objeto de contato para JSON
    var message = System.Text.Json.JsonSerializer.Serialize(contact);
    var body = Encoding.UTF8.GetBytes(message);

    // Publica a mensagem na fila
    channel.BasicPublish(exchange: "",
                         routingKey: "contact_update_queue",
                         basicProperties: null,
                         body: body);

    return Results.Ok($"Contato com nome {name} enviado para a fila de atualiza��o.");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// DTO simples para o contato
public class ContactDto
{
    [Required(ErrorMessage = "Nome � obrigat�rio")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string Nome { get; set; }

    [Required(ErrorMessage = "Telefone � obrigat�rio")]
    [Phone(ErrorMessage = "Formato de telefone inv�lido")]
    public string Telefone { get; set; }

    [Required(ErrorMessage = "Email � obrigat�rio")]
    [EmailAddress(ErrorMessage = "Formato de email inv�lido")]
    public string Email { get; set; }
}