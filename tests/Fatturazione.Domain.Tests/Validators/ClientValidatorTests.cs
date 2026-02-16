using Fatturazione.Domain.Models;
using Fatturazione.Domain.Validators;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Validators;

/// <summary>
/// Tests for ClientValidator per Art. 21, comma 2, lett. e-f, DPR 633/72
/// </summary>
public class ClientValidatorTests
{
    private static Client CreateValidClient() => new()
    {
        RagioneSociale = "Studio Rossi & Associati",
        PartitaIva = "12345678903",
        CodiceFiscale = "RSSMRA70A01H501Z",
        ClientType = ClientType.Professional,
        Email = "info@studiorossi.it",
        Address = new Address
        {
            Street = "Via Roma 123",
            City = "Milano",
            Province = "MI",
            PostalCode = "20121",
            Country = "Italia"
        }
    };

    [Fact]
    public void Validate_WithValidClient_ReturnsTrue()
    {
        var client = CreateValidClient();
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyRagioneSociale_ReturnsError()
    {
        var client = CreateValidClient();
        client.RagioneSociale = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("RagioneSociale è obbligatoria");
    }

    [Fact]
    public void Validate_WithEmptyEmail_ReturnsError()
    {
        var client = CreateValidClient();
        client.Email = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Email è obbligatoria");
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsError()
    {
        var client = CreateValidClient();
        client.Email = "not-an-email";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Email non è in un formato valido");
    }

    [Fact]
    public void Validate_WithValidCodiceFiscalePersonaFisica_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CodiceFiscale = "RSSMRA70A01H501Z";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidCodiceFiscalePersonaGiuridica_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CodiceFiscale = "12345678901";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidCodiceFiscale_ReturnsError()
    {
        var client = CreateValidClient();
        client.CodiceFiscale = "INVALID";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceFiscale deve essere 16 caratteri alfanumerici (persona fisica) o 11 cifre (persona giuridica)");
    }

    [Fact]
    public void Validate_WithNullCodiceFiscale_IsValid()
    {
        var client = CreateValidClient();
        client.CodiceFiscale = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidPostalCode_ReturnsError()
    {
        var client = CreateValidClient();
        client.Address.PostalCode = "1234"; // Too short
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Address.PostalCode deve essere esattamente 5 cifre numeriche");
    }

    [Fact]
    public void Validate_WithLettersInPostalCode_ReturnsError()
    {
        var client = CreateValidClient();
        client.Address.PostalCode = "2012A";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Address.PostalCode deve essere esattamente 5 cifre numeriche");
    }

    [Fact]
    public void Validate_WithInvalidProvince_ReturnsError()
    {
        var client = CreateValidClient();
        client.Address.Province = "MIL"; // Too long
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Address.Province deve essere esattamente 2 lettere maiuscole (es. MI, RM)");
    }

    [Fact]
    public void Validate_WithLowercaseProvince_ReturnsError()
    {
        var client = CreateValidClient();
        client.Address.Province = "mi";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Address.Province deve essere esattamente 2 lettere maiuscole (es. MI, RM)");
    }

    [Fact]
    public void Validate_WithEmptyStreet_ReturnsError()
    {
        var client = CreateValidClient();
        client.Address.Street = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Address.Street è obbligatorio");
    }

    [Fact]
    public void Validate_WithEmptyCity_ReturnsError()
    {
        var client = CreateValidClient();
        client.Address.City = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Address.City è obbligatorio");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var client = new Client
        {
            RagioneSociale = "",
            Email = "",
            Address = new Address
            {
                Street = "",
                City = "",
                PostalCode = "123",
                Province = "x"
            }
        };

        var (isValid, errors) = ClientValidator.Validate(client);

        isValid.Should().BeFalse();
        errors.Count.Should().BeGreaterThanOrEqualTo(6);
    }

    #region PA-specific validations

    [Fact]
    public void Validate_PA_WithValidCodiceUnivocoUfficio_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.PublicAdministration;
        client.CodiceUnivocoUfficio = "ABC123";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_PA_WithMissingCodiceUnivocoUfficio_ReturnsError()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.PublicAdministration;
        client.CodiceUnivocoUfficio = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceUnivocoUfficio è obbligatorio per clienti PA");
    }

    [Fact]
    public void Validate_PA_WithEmptyCodiceUnivocoUfficio_ReturnsError()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.PublicAdministration;
        client.CodiceUnivocoUfficio = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceUnivocoUfficio è obbligatorio per clienti PA");
    }

    [Fact]
    public void Validate_PA_WithInvalidCodiceUnivocoUfficio_ReturnsError()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.PublicAdministration;
        client.CodiceUnivocoUfficio = "AB"; // Too short
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceUnivocoUfficio deve essere 6 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_NonPA_WithoutCodiceUnivocoUfficio_IsValid()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.Professional;
        client.CodiceUnivocoUfficio = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    #endregion
}
