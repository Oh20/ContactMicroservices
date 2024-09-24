﻿using Moq;
using RabbitMQ.Client;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

[TestFixture]
public class ContactCreateUpdateServiceTests
{
    private Mock<IConnectionFactory> _connectionFactoryMock;
    private Mock<IModel> _channelMock;
    private Mock<IConnection> _connectionMock;

    [SetUp]
    public void Setup()
    {
        _connectionFactoryMock = new Mock<IConnectionFactory>();
        _channelMock = new Mock<IModel>();
        _connectionMock = new Mock<IConnection>();

        // Setup para mocks de RabbitMQ
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_connectionMock.Object);
        _connectionMock.Setup(c => c.CreateModel()).Returns(_channelMock.Object);
    }

    [Test]
    public async Task PostContact_ShouldReturnOk_WhenContactIsValid()
    {
        // Arrange
        var contact = new ContactDto
        {
            Nome = "Antony Matias",
            Telefone = "123456789",
            Email = "john@example.com"
        };

        var httpContext = new DefaultHttpContext();

        // Act
        var result = await new Func<Task<IResult>>(() =>
            Task.FromResult(PostContact(contact, httpContext))).Invoke();

        // Assert
        Assert.IsInstanceOf<Ok<string>>(result);
        var okResult = result as Ok<string>;
        Assert.AreEqual("Contato direcionado à fila de Criação", okResult?.Value);
    }

    [Test]
    public void PostContact_ShouldReturnBadRequest_WhenContactIsInvalid()
    {
        // Arrange
        var contact = new ContactDto
        {
            Nome = "",
            Telefone = "",
            Email = "invalid-email"
        };

        var httpContext = new DefaultHttpContext();
        var validationContext = new ValidationContext(contact);

        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(contact, validationContext, validationResults, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.AreEqual(3, validationResults.Count); // Verifica se 3 erros foram capturados
    }

    [Test]
    public void PutContact_ShouldReturnOk_WhenContactIsValid()
    {
        // Arrange
        var contact = new ContactDto
        {
            Nome = "John Doe",
            Telefone = "123456789",
            Email = "john@example.com"
        };

        // Act
        var result = PutContact("John Doe", contact);

        // Assert
        Assert.IsInstanceOf<Ok<string>>(result);
        var okResult = result as Ok<string>;
        Assert.AreEqual("Contato com nome John Doe enviado para a fila de atualização.", okResult?.Value);
    }

    [Test]
    public void PutContact_ShouldReturnBadRequest_WhenContactIsInvalid()
    {
        // Arrange
        var contact = new ContactDto
        {
            Nome = "",
            Telefone = "",
            Email = "invalid-email"
        };

        // Act
        var result = PutContact("John Doe", contact);

        // Assert
        Assert.IsInstanceOf<BadRequest<string>>(result);
    }

    // Métodos de simulação (emular o comportamento da API minimalista)
    public static IResult PostContact(ContactDto contact, HttpContext httpContext)
    {
        // Validação de modelo
        var validationContext = new ValidationContext(contact, null, null);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(contact, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(v => v.ErrorMessage).ToList();
            return Results.BadRequest(new { Errors = errors });
        }

        // Simulação de envio ao RabbitMQ
        var message = System.Text.Json.JsonSerializer.Serialize(contact);
        var body = Encoding.UTF8.GetBytes(message);

        // Normalmente, publicaria aqui para a fila, mas para o teste, apenas retornamos sucesso
        return Results.Ok("Contato direcionado à fila de Criação");
    }

    public static IResult PutContact(string name, ContactDto contact)
    {
        // Validação de campos
        if (string.IsNullOrWhiteSpace(contact.Nome) || string.IsNullOrWhiteSpace(contact.Telefone) || string.IsNullOrWhiteSpace(contact.Email))
        {
            return Results.BadRequest("Nome, Telefone ou Email não podem estar vazios.");
        }

        // Atualiza o nome do contato
        contact.Nome = name;

        // Simulação de envio ao RabbitMQ
        var message = System.Text.Json.JsonSerializer.Serialize(contact);
        var body = Encoding.UTF8.GetBytes(message);

        // Normalmente, publicaria aqui para a fila, mas para o teste, apenas retornamos sucesso
        return Results.Ok($"Contato com nome {name} enviado para a fila de atualização.");
    }
}
