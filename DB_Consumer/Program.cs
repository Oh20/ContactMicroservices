using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

consumer.Received += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    Console.WriteLine($"Received message: {message}");

    // Desserializar a mensagem para o objeto ContactDto
    var contactDto = System.Text.Json.JsonSerializer.Deserialize<ContactDto>(message);

    if (contactDto != null)
    {
        // Validar manualmente as Data Annotations
        var validationContext = new ValidationContext(contactDto);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(contactDto, validationContext, validationResults, true);

        if (!isValid)
        {
            foreach (var validationResult in validationResults)
            {
                Console.WriteLine($"Validation failed: {validationResult.ErrorMessage}");
            }
            return; // Se falhar, não salvar no banco
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Mapear o DTO para o modelo Contact
        var contact = new Contact { Nome = contactDto.Nome, Telefone = contactDto.Telefone, Email = contactDto.Email };

        // Verificar duplicação de e-mail ou telefone
        var existingContact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Email == contact.Email || c.Telefone == contact.Telefone);

        if (existingContact != null)
        {
            Console.WriteLine($"Duplicate contact found for email: {contact.Email} or phone: {contact.Telefone}");
            return; // Se duplicado, não salvar
        }

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
    [Required(ErrorMessage = "O campo Nome é obrigatório.")]
    public string Nome { get; set; }

    [Required(ErrorMessage = "O campo Telefone é obrigatório.")]
    [Phone(ErrorMessage = "Formato de telefone inválido.")]
    public string Telefone { get; set; }

    [Required(ErrorMessage = "O campo Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de email inválido.")]
    public string Email { get; set; }
}