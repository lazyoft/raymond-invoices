using Fatturazione.Domain.Validators;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Validators;

/// <summary>
/// Tests for CodiceFiscaleValidator -- Italian Codice Fiscale checksum algorithm
/// per DM 12/03/1974
/// </summary>
public class CodiceFiscaleValidatorTests
{
    #region Valid Codice Fiscale values

    [Fact]
    public void ValidateChecksum_RSSMRA70A01H501S_ReturnsTrue()
    {
        // Valid Italian Codice Fiscale (Mario Rossi, born 01/01/1970, Roma)
        // Check char S computed from algorithm: sum=96, 96 mod 26=18, 18='S'
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01H501S").Should().BeTrue();
    }

    [Fact]
    public void ValidateChecksum_Lowercase_ReturnsTrue()
    {
        // The algorithm should work case-insensitively
        CodiceFiscaleValidator.ValidateChecksum("rssmra70a01h501s").Should().BeTrue();
    }

    [Fact]
    public void ValidateChecksum_MixedCase_ReturnsTrue()
    {
        CodiceFiscaleValidator.ValidateChecksum("RssMra70A01h501S").Should().BeTrue();
    }

    #endregion

    #region Invalid check character

    [Fact]
    public void ValidateChecksum_WrongCheckChar_Z_ReturnsFalse()
    {
        // RSSMRA70A01H501S is valid (S); Z is incorrect
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01H501Z").Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_WrongCheckChar_A_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01H501A").Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_WrongCheckChar_B_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01H501B").Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_WrongCheckChar_Y_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01H501Y").Should().BeFalse();
    }

    #endregion

    #region Invalid input

    [Fact]
    public void ValidateChecksum_NullInput_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum(null!).Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_EmptyString_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum("").Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_TooShort_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01").Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_TooLong_ReturnsFalse()
    {
        CodiceFiscaleValidator.ValidateChecksum("RSSMRA70A01H501SX").Should().BeFalse();
    }

    [Fact]
    public void ValidateChecksum_11Digits_ReturnsFalse()
    {
        // 11-digit CF (persona giuridica) should not be validated by this method
        CodiceFiscaleValidator.ValidateChecksum("12345678901").Should().BeFalse();
    }

    #endregion

    #region GetValidationError

    [Fact]
    public void GetValidationError_ValidCF_ReturnsEmptyString()
    {
        CodiceFiscaleValidator.GetValidationError("RSSMRA70A01H501S").Should().BeEmpty();
    }

    [Fact]
    public void GetValidationError_InvalidCheckChar_ReturnsErrorMessage()
    {
        var error = CodiceFiscaleValidator.GetValidationError("RSSMRA70A01H501Z");
        error.Should().Be("Codice Fiscale non valido (carattere di controllo errato)");
    }

    [Fact]
    public void GetValidationError_NullInput_ReturnsErrorMessage()
    {
        var error = CodiceFiscaleValidator.GetValidationError(null!);
        error.Should().Be("Codice Fiscale non valido (carattere di controllo errato)");
    }

    #endregion
}
