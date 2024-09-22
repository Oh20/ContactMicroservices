using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
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

app.MapPost("/contacts", ([FromBody] ContactDto contact) =>
{
    // Configuração do RabbitMQ
    var factory = new ConnectionFactory() { HostName = "localhost" }; // Substitua pelo hostname do seu RabbitMQ
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

    return Results.Ok("Contact created/updated and sent to the queue");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// DTO simples para o contato
public class ContactDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
}