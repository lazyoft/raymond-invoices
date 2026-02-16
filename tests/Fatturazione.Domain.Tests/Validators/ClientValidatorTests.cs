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
        CodiceFiscale = "RSSMRA70A01H501S",
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
        client.CodiceFiscale = "RSSMRA70A01H501S";
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
    public void Validate_WithCodiceFiscaleWrongCheckDigit_ReturnsError()
    {
        var client = CreateValidClient();
        // RSSMRA70A01H501S is valid (check char S); Z is incorrect
        client.CodiceFiscale = "RSSMRA70A01H501Z";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Codice Fiscale non valido (carattere di controllo errato)");
    }

    [Fact]
    public void Validate_WithCodiceFiscaleLowercaseValidChecksum_ReturnsTrue()
    {
        var client = CreateValidClient();
        // Lowercase version of a valid CF should still pass (case-insensitive)
        client.CodiceFiscale = "rssmra70a01h501s";
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

    #region CIG validation (Art. 25 DL 66/2014)

    [Fact]
    public void Validate_WithValidCIG_10Alphanumeric_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CIG = "ABC1234567";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCIG_AllDigits_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CIG = "0123456789";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCIG_AllLetters_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CIG = "ABCDEFGHIJ";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCIG_TooShort_ReturnsError()
    {
        var client = CreateValidClient();
        client.CIG = "ABC123"; // 6 chars, need 10
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CIG deve essere esattamente 10 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithCIG_TooLong_ReturnsError()
    {
        var client = CreateValidClient();
        client.CIG = "ABC12345678"; // 11 chars, need 10
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CIG deve essere esattamente 10 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithCIG_SpecialChars_ReturnsError()
    {
        var client = CreateValidClient();
        client.CIG = "ABC-12345!"; // contains special chars
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CIG deve essere esattamente 10 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithNullCIG_IsValid()
    {
        var client = CreateValidClient();
        client.CIG = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCIG_IsValid()
    {
        var client = CreateValidClient();
        client.CIG = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    #endregion

    #region CUP validation (Legge 136/2010)

    [Fact]
    public void Validate_WithValidCUP_15Alphanumeric_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CUP = "ABC123456789012";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCUP_AllDigits_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CUP = "012345678901234";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCUP_AllLetters_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CUP = "ABCDEFGHIJKLMNO";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCUP_TooShort_ReturnsError()
    {
        var client = CreateValidClient();
        client.CUP = "ABC123"; // 6 chars, need 15
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CUP deve essere esattamente 15 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithCUP_TooLong_ReturnsError()
    {
        var client = CreateValidClient();
        client.CUP = "ABC1234567890123"; // 16 chars, need 15
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CUP deve essere esattamente 15 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithCUP_SpecialChars_ReturnsError()
    {
        var client = CreateValidClient();
        client.CUP = "ABC-12345678!0@"; // contains special chars
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CUP deve essere esattamente 15 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithNullCUP_IsValid()
    {
        var client = CreateValidClient();
        client.CUP = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCUP_IsValid()
    {
        var client = CreateValidClient();
        client.CUP = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    #endregion

    #region CodiceDestinatario validation (DL 127/2015)

    [Fact]
    public void Validate_WithValidCodiceDestinatario_7Alphanumeric_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = "ABC1234";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCodiceDestinatario_AllZeros_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = "0000000";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCodiceDestinatario_TooShort_ReturnsError()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = "ABC12"; // 5 chars, need 7
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceDestinatario deve essere esattamente 7 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithCodiceDestinatario_TooLong_ReturnsError()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = "ABC12345"; // 8 chars, need 7
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceDestinatario deve essere esattamente 7 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithCodiceDestinatario_SpecialChars_ReturnsError()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = "ABC-12!"; // contains special chars
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("CodiceDestinatario deve essere esattamente 7 caratteri alfanumerici");
    }

    [Fact]
    public void Validate_WithNullCodiceDestinatario_IsValid()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCodiceDestinatario_IsValid()
    {
        var client = CreateValidClient();
        client.CodiceDestinatario = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    #endregion

    #region PEC validation

    [Fact]
    public void Validate_WithValidPEC_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.PEC = "fatturazione@pec.studiorossi.it";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithPEC_SimpleDomain_ReturnsTrue()
    {
        var client = CreateValidClient();
        client.PEC = "info@pec.it";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithPEC_InvalidFormat_NoAtSign_ReturnsError()
    {
        var client = CreateValidClient();
        client.PEC = "not-a-pec-address";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("PEC non è in un formato email valido");
    }

    [Fact]
    public void Validate_WithPEC_InvalidFormat_NoDomain_ReturnsError()
    {
        var client = CreateValidClient();
        client.PEC = "user@";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("PEC non è in un formato email valido");
    }

    [Fact]
    public void Validate_WithPEC_InvalidFormat_NoTLD_ReturnsError()
    {
        var client = CreateValidClient();
        client.PEC = "user@domain";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("PEC non è in un formato email valido");
    }

    [Fact]
    public void Validate_WithNullPEC_IsValid()
    {
        var client = CreateValidClient();
        client.PEC = null;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyPEC_IsValid()
    {
        var client = CreateValidClient();
        client.PEC = "";
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    #endregion

    #region Split payment validation (Art. 17-ter DPR 633/72)

    [Fact]
    public void Validate_SplitPayment_PA_IsValid()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.PublicAdministration;
        client.CodiceUnivocoUfficio = "ABC123";
        client.SubjectToSplitPayment = true;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_SplitPayment_Professional_ReturnsError()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.Professional;
        client.SubjectToSplitPayment = true;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Split payment (scissione dei pagamenti) è applicabile solo a clienti di tipo Pubblica Amministrazione");
    }

    [Fact]
    public void Validate_SplitPayment_Company_ReturnsError()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.Company;
        client.SubjectToSplitPayment = true;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeFalse();
        errors.Should().Contain("Split payment (scissione dei pagamenti) è applicabile solo a clienti di tipo Pubblica Amministrazione");
    }

    [Fact]
    public void Validate_NoSplitPayment_Professional_IsValid()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.Professional;
        client.SubjectToSplitPayment = false;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoSplitPayment_Company_IsValid()
    {
        var client = CreateValidClient();
        client.ClientType = ClientType.Company;
        client.SubjectToSplitPayment = false;
        var (isValid, errors) = ClientValidator.Validate(client);
        isValid.Should().BeTrue();
    }

    #endregion
}
