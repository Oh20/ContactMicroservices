using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar o DbContext para o SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Obtenha as variáveis de ambiente para a conexão com o RabbitMQ
var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
var rabbitMqPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
var rabbitMqUser = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";

// Configurar RabbitMQ
var factory = new ConnectionFactory()
{
    HostName = rabbitMqHost,
    Port = rabbitMqPort,
    UserName = rabbitMqUser,
    Password = rabbitMqPassword
};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

//Fila de criação
channel.QueueDeclare(queue: "contact_queue",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

var consumer = new EventingBasicConsumer(channel);

// Consumir as mensagens da fila
consumer.Received += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    Console.WriteLine($"Received message: {message}");

    // Desserializar a mensagem para o objeto ContactDto
    var contactDto = System.Text.Json.JsonSerializer.Deserialize<ContactDto>(message);

    if (contactDto != null)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Mapear o DTO para o modelo Contact
        var contact = new Contact
        {
            Name = contactDto.Name,
            Phone = contactDto.Phone,
            Email = contactDto.Email
        };

        // Salvar o contato no banco de dados
        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync();

        Console.WriteLine("Contact saved to the database.");
    }
};

channel.BasicConsume(queue: "contact_queue",
                     autoAck: true,
                     consumer: consumer);

Console.WriteLine("DBConsumer is waiting for messages.");

// Consumir fila de exclusão
channel.QueueDeclare(queue: "delete_contact_queue",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

var deleteConsumer = new EventingBasicConsumer(channel);
deleteConsumer.Received += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    Console.WriteLine($"Received delete message: {message}");

    if (int.TryParse(message, out var contactId))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var contact = await dbContext.Contacts.FindAsync(contactId);
        if (contact != null)
        {
            dbContext.Contacts.Remove(contact);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"Contact with ID {contactId} deleted from the database.");
        }
        else
        {
            Console.WriteLine($"Contact with ID {contactId} not found.");
        }
    }
};

channel.BasicConsume(queue: "delete_contact_queue", autoAck: true, consumer: deleteConsumer);

Console.WriteLine("DBConsumer is waiting for messages.");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.Run();

// DTO utilizado para desserialização
public class ContactDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
}
