using Microsoft.AspNetCore.Mvc;
using Prometheus;
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
    // Validação de modelo
    var validationContext = new ValidationContext(contact, null, null);
    var validationResults = new List<ValidationResult>();

    if (!Validator.TryValidateObject(contact, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { Errors = errors });
    }

    // Configuração do RabbitMQ
    var factory = new ConnectionFactory() { HostName = "52.191.9.118" };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // Declaração da fila
    channel.QueueDeclare(queue: "contact_queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    // Serialização do objeto de contato para JSON
    var message = System.Text.Json.JsonSerializer.Serialize(contact);
    var body = Encoding.UTF8.GetBytes(message);

    // Publica a mensagem na fila
    channel.BasicPublish(exchange: "",
                         routingKey: "contact_queue",
                         basicProperties: null,
                         body: body);

    return Results.Ok("Contato direcionado à fila de Criação");
});

app.MapPut("/contacts/{name}", (string name, ContactDto contact) =>
{
    if (string.IsNullOrWhiteSpace(contact.Nome) || string.IsNullOrWhiteSpace(contact.Telefone) || string.IsNullOrWhiteSpace(contact.Email))
    {
        return Results.BadRequest("Nome, Telefone ou Email não podem estar vazios.");
    }

    contact.Nome = name;

    using var connection = new ConnectionFactory() { HostName = "52.191.9.118" }.CreateConnection();
    using var channel = connection.CreateModel();

    var rabbitMqChannel = new RabbitMqChannel(channel);
    rabbitMqChannel.DeclareQueue("contact_update_queue");

    var message = System.Text.Json.JsonSerializer.Serialize(contact);
    rabbitMqChannel.Publish("contact_update_queue", message);

    return Results.Ok($"Contato com nome {name} enviado para a fila de atualização.");
});

// Adicione o middleware para expor métricas
app.UseHttpMetrics();  // Coleta de métricas HTTP automáticas

// Exponha o endpoint /metrics
app.MapMetrics();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// DTO simples para o contato
public class ContactDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string Nome { get; set; }

    [Required(ErrorMessage = "Telefone é obrigatório")]
    [Phone(ErrorMessage = "Formato de telefone inválido")]
    public string Telefone { get; set; }

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; }
}