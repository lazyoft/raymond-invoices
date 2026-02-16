using System.Net;
using System.Net.Http.Json;
using Fatturazione.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fatturazione.Api.Tests.Endpoints;

/// <summary>
/// Integration tests for IssuerProfile endpoints
/// Art. 21, co. 2, lett. c-d, DPR 633/72
/// </summary>
public class IssuerProfileEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IssuerProfileEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private static IssuerProfile CreateValidProfile() => new()
    {
        RagioneSociale = "Studio Bianchi S.r.l.",
        PartitaIva = "12345678903",
        CodiceFiscale = "12345678903",
        RegimeFiscale = "RF01",
        Telefono = "+39 02 1234567",
        Email = "info@studiobianchi.it",
        Indirizzo = new Address
        {
            Street = "Via Roma 10",
            City = "Milano",
            Province = "MI",
            PostalCode = "20121",
            Country = "Italia"
        }
    };

    [Fact]
    public async Task GetIssuerProfile_WhenNoProfile_ReturnsNotFound()
    {
        // Use a fresh factory to guarantee no profile exists
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/issuer-profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        var profile = CreateValidProfile();

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await response.Content.ReadFromJsonAsync<IssuerProfile>();
        saved.Should().NotBeNull();
        saved!.RagioneSociale.Should().Be(profile.RagioneSociale);
        saved.PartitaIva.Should().Be(profile.PartitaIva);
        saved.RegimeFiscale.Should().Be(profile.RegimeFiscale);
        saved.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetIssuerProfile_AfterSave_ReturnsOkWithData()
    {
        // Arrange - save a profile first
        var profile = CreateValidProfile();
        await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Act
        var response = await _client.GetAsync("/api/issuer-profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrieved = await response.Content.ReadFromJsonAsync<IssuerProfile>();
        retrieved.Should().NotBeNull();
        retrieved!.RagioneSociale.Should().Be(profile.RagioneSociale);
        retrieved.PartitaIva.Should().Be(profile.PartitaIva);
        retrieved.RegimeFiscale.Should().Be(profile.RegimeFiscale);
        retrieved.Email.Should().Be(profile.Email);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithEmptyRagioneSociale_ReturnsBadRequest()
    {
        // Arrange
        var profile = CreateValidProfile();
        profile.RagioneSociale = "";

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithEmptyPartitaIva_ReturnsBadRequest()
    {
        // Arrange
        var profile = CreateValidProfile();
        profile.PartitaIva = "";

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithInvalidPartitaIva_ReturnsBadRequest()
    {
        // Arrange
        var profile = CreateValidProfile();
        profile.PartitaIva = "12345"; // Too short

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithInvalidRegimeFiscale_ReturnsBadRequest()
    {
        // Arrange
        var profile = CreateValidProfile();
        profile.RegimeFiscale = "INVALID";

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var profile = CreateValidProfile();
        profile.Email = "not-an-email";

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveIssuerProfile_WithoutEmail_ReturnsOk()
    {
        // Arrange - Email is optional
        var profile = CreateValidProfile();
        profile.Email = null;

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveIssuerProfile_UpdatesExistingProfile()
    {
        // Arrange - save initial profile
        var profile = CreateValidProfile();
        await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Act - update with new data
        var updatedProfile = CreateValidProfile();
        updatedProfile.RagioneSociale = "Studio Aggiornato S.r.l.";
        updatedProfile.RegimeFiscale = "RF19"; // Forfettario
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", updatedProfile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync("/api/issuer-profile");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<IssuerProfile>();
        retrieved!.RagioneSociale.Should().Be("Studio Aggiornato S.r.l.");
        retrieved.RegimeFiscale.Should().Be("RF19");
    }

    [Fact]
    public async Task SaveIssuerProfile_WithMissingAddress_ReturnsBadRequest()
    {
        // Arrange
        var profile = CreateValidProfile();
        profile.Indirizzo = new Address { Street = "", City = "" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/issuer-profile", profile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
