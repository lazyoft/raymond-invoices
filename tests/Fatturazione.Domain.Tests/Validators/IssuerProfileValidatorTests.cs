using Fatturazione.Domain.Models;
using Fatturazione.Domain.Validators;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Validators;

/// <summary>
/// Tests for IssuerProfileValidator per Art. 21, co. 2, lett. c-d, DPR 633/72
/// Validates the issuer (cedente/prestatore) mandatory fields
/// </summary>
public class IssuerProfileValidatorTests
{
    private static IssuerProfile CreateValidProfile() => new()
    {
        RagioneSociale = "Studio Bianchi S.r.l.",
        PartitaIva = "12345678903", // Valid Partita IVA with correct checksum
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

    #region RagioneSociale

    [Fact]
    public void Validate_WithValidProfile_ReturnsTrue()
    {
        var profile = CreateValidProfile();
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyRagioneSociale_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.RagioneSociale = "";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("RagioneSociale è obbligatoria");
    }

    [Fact]
    public void Validate_WithWhitespaceRagioneSociale_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.RagioneSociale = "   ";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("RagioneSociale è obbligatoria");
    }

    #endregion

    #region PartitaIva

    [Fact]
    public void Validate_WithEmptyPartitaIva_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.PartitaIva = "";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("PartitaIva è obbligatoria");
    }

    [Fact]
    public void Validate_WithInvalidPartitaIva_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.PartitaIva = "12345"; // Too short
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().ContainMatch("PartitaIva non valida:*");
    }

    [Fact]
    public void Validate_WithInvalidChecksumPartitaIva_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.PartitaIva = "12345678901"; // Invalid checksum
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().ContainMatch("PartitaIva non valida:*");
    }

    #endregion

    #region Indirizzo

    [Fact]
    public void Validate_WithEmptyStreet_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.Indirizzo.Street = "";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("Indirizzo.Street è obbligatorio");
    }

    [Fact]
    public void Validate_WithEmptyCity_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.Indirizzo.City = "";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("Indirizzo.City è obbligatorio");
    }

    [Fact]
    public void Validate_WithNullIndirizzo_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.Indirizzo = null!;
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("Indirizzo è obbligatorio");
    }

    #endregion

    #region RegimeFiscale

    [Theory]
    [InlineData("RF01")]
    [InlineData("RF02")]
    [InlineData("RF10")]
    [InlineData("RF19")]
    public void Validate_WithValidRegimeFiscale_ReturnsTrue(string regime)
    {
        var profile = CreateValidProfile();
        profile.RegimeFiscale = regime;
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("RF00")]
    [InlineData("RF20")]
    [InlineData("INVALID")]
    [InlineData("RF1")]
    [InlineData("XX01")]
    public void Validate_WithInvalidRegimeFiscale_ReturnsError(string regime)
    {
        var profile = CreateValidProfile();
        profile.RegimeFiscale = regime;
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("RegimeFiscale"));
    }

    #endregion

    #region Email

    [Fact]
    public void Validate_WithNullEmail_IsValid()
    {
        var profile = CreateValidProfile();
        profile.Email = null;
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyEmail_IsValid()
    {
        var profile = CreateValidProfile();
        profile.Email = "";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidEmail_IsValid()
    {
        var profile = CreateValidProfile();
        profile.Email = "test@example.com";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsError()
    {
        var profile = CreateValidProfile();
        profile.Email = "not-an-email";
        var (isValid, errors) = IssuerProfileValidator.Validate(profile);
        isValid.Should().BeFalse();
        errors.Should().Contain("Email non è in un formato valido");
    }

    #endregion

    #region Multiple errors

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var profile = new IssuerProfile
        {
            RagioneSociale = "",
            PartitaIva = "",
            RegimeFiscale = "INVALID",
            Email = "not-valid",
            Indirizzo = new Address
            {
                Street = "",
                City = ""
            }
        };

        var (isValid, errors) = IssuerProfileValidator.Validate(profile);

        isValid.Should().BeFalse();
        errors.Count.Should().BeGreaterThanOrEqualTo(5);
    }

    #endregion
}
