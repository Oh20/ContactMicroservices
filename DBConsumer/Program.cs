// Configurar o DbContext para o SQL Server
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configurar RabbitMQ
var factory = new ConnectionFactory() { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

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
            Phone = contactDto.Phone
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