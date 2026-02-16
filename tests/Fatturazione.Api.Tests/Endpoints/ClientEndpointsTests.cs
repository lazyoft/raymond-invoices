using System.Net;
using System.Net.Http.Json;
using Fatturazione.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fatturazione.Api.Tests.Endpoints;

/// <summary>
/// Integration tests for Client endpoints
/// </summary>
public class ClientEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ClientEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllClients_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
        clients.Should().NotBeNull();
        clients.Should().HaveCountGreaterThan(0); // Seed data should have 4 clients
    }

    [Fact]
    public async Task GetClientById_WithValidId_ReturnsOk()
    {
        // Arrange - Get a client from seed data first
        var allClientsResponse = await _client.GetAsync("/api/clients");
        var clients = await allClientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var existingClient = clients!.First();

        // Act
        var response = await _client.GetAsync($"/api/clients/{existingClient.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var client = await response.Content.ReadFromJsonAsync<Client>();
        client.Should().NotBeNull();
        client!.Id.Should().Be(existingClient.Id);
    }

    [Fact]
    public async Task GetClientById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/clients/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClient_WithValidData_ReturnsCreated()
    {
        // Arrange
        var newClient = new Client
        {
            RagioneSociale = "Test Company S.r.l.",
            PartitaIva = "01234567897", // Valid Partita IVA with correct checksum
            CodiceFiscale = "12345678901", // Valid persona giuridica format (11 digits)
            ClientType = ClientType.Company,
            Email = "test@example.com",
            Address = new Address
            {
                Street = "Via Test 123",
                City = "Milano",
                Province = "MI",
                PostalCode = "20100",
                Country = "Italia"
            },
            SubjectToRitenuta = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clients", newClient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdClient = await response.Content.ReadFromJsonAsync<Client>();
        createdClient.Should().NotBeNull();
        createdClient!.Id.Should().NotBe(Guid.Empty);
        createdClient.RagioneSociale.Should().Be(newClient.RagioneSociale);
    }

    [Fact]
    public async Task CreateClient_WithInvalidPartitaIva_ReturnsBadRequest()
    {
        // Arrange
        var invalidClient = new Client
        {
            RagioneSociale = "Invalid Company",
            PartitaIva = "12345", // Too short
            Email = "invalid@example.com",
            ClientType = ClientType.Company,
            Address = new Address()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clients", invalidClient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateClient_WithDuplicatePartitaIva_ReturnsBadRequest()
    {
        // Arrange - Get an existing client's Partita IVA
        var allClientsResponse = await _client.GetAsync("/api/clients");
        var clients = await allClientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var existingPartitaIva = clients!.First().PartitaIva;

        var duplicateClient = new Client
        {
            RagioneSociale = "Duplicate Company",
            PartitaIva = existingPartitaIva,
            Email = "duplicate@example.com",
            ClientType = ClientType.Company,
            Address = new Address()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clients", duplicateClient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateClient_WithValidData_ReturnsOk()
    {
        // Arrange - First create a client
        var newClient = new Client
        {
            RagioneSociale = "Update Test Company",
            PartitaIva = "09876543217", // Valid checksum
            Email = "update@example.com",
            ClientType = ClientType.Company,
            Address = new Address
            {
                Street = "Via Update 1",
                City = "Roma",
                Province = "RM",
                PostalCode = "00100",
                Country = "Italia"
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/clients", newClient);
        var createdClient = await createResponse.Content.ReadFromJsonAsync<Client>();

        // Modify the client
        createdClient!.RagioneSociale = "Updated Company Name";

        // Act
        var response = await _client.PutAsJsonAsync($"/api/clients/{createdClient.Id}", createdClient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedClient = await response.Content.ReadFromJsonAsync<Client>();
        updatedClient!.RagioneSociale.Should().Be("Updated Company Name");
    }

    [Fact]
    public async Task UpdateClient_WithIdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var client = new Client
        {
            Id = Guid.NewGuid(),
            RagioneSociale = "Test",
            PartitaIva = "09876543217",
            Email = "test@example.com",
            ClientType = ClientType.Company,
            Address = new Address()
        };

        var differentId = Guid.NewGuid();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/clients/{differentId}", client);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateClient_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var client = new Client
        {
            Id = nonExistentId,
            RagioneSociale = "Non-existent",
            PartitaIva = "09876543217",
            Email = "test@example.com",
            ClientType = ClientType.Company,
            Address = new Address()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/clients/{nonExistentId}", client);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteClient_WithValidId_ReturnsNoContent()
    {
        // Arrange - First create a client to delete
        var newClient = new Client
        {
            RagioneSociale = "To Delete Company",
            PartitaIva = "22222222220", // Valid checksum, unique for this test
            Email = "delete@example.com",
            ClientType = ClientType.Company,
            Address = new Address
            {
                Street = "Via Delete 1",
                City = "Roma",
                Province = "RM",
                PostalCode = "00100",
                Country = "Italia"
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/clients", newClient);
        var createdClient = await createResponse.Content.ReadFromJsonAsync<Client>();

        // Act
        var response = await _client.DeleteAsync($"/api/clients/{createdClient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteClient_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/clients/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ValidatePartitaIva_WithValidNumber_ReturnsValid()
    {
        // Arrange
        var validPartitaIva = "01234567897";

        // Act
        var response = await _client.GetAsync($"/api/clients/validate-partita-iva/{validPartitaIva}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ValidationResult>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ValidatePartitaIva_WithInvalidNumber_ReturnsInvalid()
    {
        // Arrange
        var invalidPartitaIva = "12345";

        // Act
        var response = await _client.GetAsync($"/api/clients/validate-partita-iva/{invalidPartitaIva}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ValidationResult>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
}
