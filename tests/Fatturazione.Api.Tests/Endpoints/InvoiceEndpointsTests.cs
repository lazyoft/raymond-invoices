using System.Net;
using System.Net.Http.Json;
using Fatturazione.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fatturazione.Api.Tests.Endpoints;

/// <summary>
/// Integration tests for Invoice endpoints
/// </summary>
public class InvoiceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public InvoiceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllInvoices_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoices = await response.Content.ReadFromJsonAsync<List<Invoice>>();
        invoices.Should().NotBeNull();
        invoices.Should().HaveCountGreaterThan(0); // Seed data should have 3 invoices
    }

    [Fact]
    public async Task GetInvoiceById_WithValidId_ReturnsOk()
    {
        // Arrange - Get an invoice from seed data first
        var allInvoicesResponse = await _client.GetAsync("/api/invoices");
        var invoices = await allInvoicesResponse.Content.ReadFromJsonAsync<List<Invoice>>();
        var existingInvoice = invoices!.First();

        // Act
        var response = await _client.GetAsync($"/api/invoices/{existingInvoice.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadFromJsonAsync<Invoice>();
        invoice.Should().NotBeNull();
        invoice!.Id.Should().Be(existingInvoice.Id);
    }

    [Fact]
    public async Task GetInvoiceById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/invoices/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoicesByClient_ReturnsInvoicesForClient()
    {
        // Arrange - Get a client ID from seed data
        var clientsResponse = await _client.GetAsync("/api/clients");
        var clients = await clientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var clientId = clients!.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/invoices/by-client/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoices = await response.Content.ReadFromJsonAsync<List<Invoice>>();
        invoices.Should().NotBeNull();
        // All returned invoices should belong to the specified client
        if (invoices != null)
        {
            invoices.Where(i => i.ClientId == clientId).Should().HaveCount(invoices.Count);
        }
    }

    [Fact]
    public async Task CreateInvoice_WithValidData_ReturnsCreatedWithCalculatedTotals()
    {
        // Arrange - Get a client first
        var clientsResponse = await _client.GetAsync("/api/clients");
        var clients = await clientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var client = clients!.First();

        var newInvoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = "Test Service",
                    Quantity = 1,
                    UnitPrice = 1000,
                    IvaRate = IvaRate.Standard
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", newInvoice);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdInvoice = await response.Content.ReadFromJsonAsync<Invoice>();
        createdInvoice.Should().NotBeNull();
        createdInvoice!.Id.Should().NotBe(Guid.Empty);
        createdInvoice.ImponibileTotal.Should().Be(1000m);
        createdInvoice.IvaTotal.Should().Be(220m);
        createdInvoice.SubTotal.Should().Be(1220m);
    }

    [Fact]
    public async Task CreateInvoice_WithNonExistentClient_ReturnsBadRequest()
    {
        // Arrange
        var newInvoice = new Invoice
        {
            ClientId = Guid.NewGuid(), // Non-existent client
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = "Test Service",
                    Quantity = 1,
                    UnitPrice = 1000,
                    IvaRate = IvaRate.Standard
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", newInvoice);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInvoice_WithValidationError_ReturnsBadRequest()
    {
        // Arrange - Invalid invoice (no ClientId, invalid dates)
        var invalidInvoice = new Invoice
        {
            ClientId = Guid.Empty,
            InvoiceDate = default,
            DueDate = default
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", invalidInvoice);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateInvoice_WithValidData_ReturnsOkAndRecalculatesTotals()
    {
        // Arrange - First create an invoice
        var clientsResponse = await _client.GetAsync("/api/clients");
        var clients = await clientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var client = clients!.First();

        var newInvoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = "Original Service",
                    Quantity = 1,
                    UnitPrice = 500,
                    IvaRate = IvaRate.Standard
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/invoices", newInvoice);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<Invoice>();

        // Modify the invoice
        createdInvoice!.Items[0].UnitPrice = 1000;

        // Act
        var response = await _client.PutAsJsonAsync($"/api/invoices/{createdInvoice.Id}", createdInvoice);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedInvoice = await response.Content.ReadFromJsonAsync<Invoice>();
        updatedInvoice!.ImponibileTotal.Should().Be(1000m);
        updatedInvoice.IvaTotal.Should().Be(220m);
    }

    [Fact]
    public async Task UpdateInvoice_WithIdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30)
        };

        var differentId = Guid.NewGuid();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/invoices/{differentId}", invoice);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateInvoiceTotals_RecalculatesTotals()
    {
        // Arrange - Get an existing invoice
        var allInvoicesResponse = await _client.GetAsync("/api/invoices");
        var invoices = await allInvoicesResponse.Content.ReadFromJsonAsync<List<Invoice>>();
        var existingInvoice = invoices!.First();

        // Act
        var response = await _client.PostAsync($"/api/invoices/{existingInvoice.Id}/calculate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadFromJsonAsync<Invoice>();
        invoice.Should().NotBeNull();
        invoice!.ImponibileTotal.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task IssueInvoice_ChangesStatusAndAssignsNumber()
    {
        // Arrange - Create a draft invoice first
        var clientsResponse = await _client.GetAsync("/api/clients");
        var clients = await clientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var client = clients!.First();

        var newInvoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Status = InvoiceStatus.Draft,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = "Service to Issue",
                    Quantity = 1,
                    UnitPrice = 1000,
                    IvaRate = IvaRate.Standard
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/invoices", newInvoice);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<Invoice>();

        // Act
        var response = await _client.PostAsync($"/api/invoices/{createdInvoice!.Id}/issue", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var issuedInvoice = await response.Content.ReadFromJsonAsync<Invoice>();
        issuedInvoice.Should().NotBeNull();
        issuedInvoice!.Status.Should().Be(InvoiceStatus.Issued);
        issuedInvoice.InvoiceNumber.Should().NotBeNullOrEmpty();
        issuedInvoice.InvoiceNumber.Should().MatchRegex(@"^\d{4}/\d{3}$");
    }

    [Fact]
    public async Task IssueInvoice_OnAlreadyIssuedInvoice_ReturnsBadRequest()
    {
        // Arrange - Get an already issued invoice from seed data
        var allInvoicesResponse = await _client.GetAsync("/api/invoices");
        var invoices = await allInvoicesResponse.Content.ReadFromJsonAsync<List<Invoice>>();
        var issuedInvoice = invoices!.FirstOrDefault(i => i.Status == InvoiceStatus.Issued);

        if (issuedInvoice == null)
        {
            // If no issued invoice in seed data, skip this test
            return;
        }

        // Act
        var response = await _client.PostAsync($"/api/invoices/{issuedInvoice.Id}/issue", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteInvoice_WithValidId_ReturnsNoContent()
    {
        // Arrange - First create an invoice to delete
        var clientsResponse = await _client.GetAsync("/api/clients");
        var clients = await clientsResponse.Content.ReadFromJsonAsync<List<Client>>();
        var client = clients!.First();

        var newInvoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = "To Delete",
                    Quantity = 1,
                    UnitPrice = 100,
                    IvaRate = IvaRate.Standard
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/invoices", newInvoice);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<Invoice>();

        // Act
        var response = await _client.DeleteAsync($"/api/invoices/{createdInvoice!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
