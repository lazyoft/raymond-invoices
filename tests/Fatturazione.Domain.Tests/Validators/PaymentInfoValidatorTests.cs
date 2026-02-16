using Fatturazione.Domain.Models;
using Fatturazione.Domain.Validators;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Validators;

/// <summary>
/// Tests for PaymentInfoValidator per Specifiche FatturaPA, blocco 2.4
/// </summary>
public class PaymentInfoValidatorTests
{
    private static PaymentInfo CreateValidPaymentInfo() => new()
    {
        Condizioni = PaymentCondition.TP02_Completo,
        Modalita = PaymentMethod.MP05_Bonifico,
        IBAN = "IT60X0542811101000000123456",
        BancaAppoggio = "Banca Intesa Sanpaolo"
    };

    #region Valid scenarios

    [Fact]
    public void Validate_WithValidPaymentInfo_ReturnsValid()
    {
        var paymentInfo = CreateValidPaymentInfo();
        var (isValid, errors, warnings) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidIBAN_ReturnsValid()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.IBAN = "IT60X0542811101000000123456";
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullIBAN_AndNonBonifico_ReturnsValid()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.Modalita = PaymentMethod.MP01_Contanti;
        paymentInfo.IBAN = null;
        var (isValid, errors, warnings) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
        warnings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(PaymentCondition.TP01_Rate)]
    [InlineData(PaymentCondition.TP02_Completo)]
    [InlineData(PaymentCondition.TP03_Anticipo)]
    public void Validate_WithValidCondizioni_ReturnsValid(PaymentCondition condizione)
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.Condizioni = condizione;
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region IBAN validation

    [Fact]
    public void Validate_WithInvalidIBAN_TooShort_ReturnsError()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.IBAN = "IT60X054281";
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("IBAN non valido"));
    }

    [Fact]
    public void Validate_WithInvalidIBAN_NotItalian_ReturnsError()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.IBAN = "DE89370400440532013000"; // German IBAN
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("IBAN non valido"));
    }

    [Fact]
    public void Validate_WithInvalidIBAN_WrongFormat_ReturnsError()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.IBAN = "IT00000000000000000000000000"; // Missing CIN letter
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("IBAN non valido"));
    }

    [Fact]
    public void Validate_WithEmptyIBAN_AndBonifico_ReturnsWarning()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.Modalita = PaymentMethod.MP05_Bonifico;
        paymentInfo.IBAN = "";
        var (isValid, errors, warnings) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue("missing IBAN with bonifico is a warning, not an error");
        errors.Should().BeEmpty();
        warnings.Should().Contain(w => w.Contains("IBAN non specificato per pagamento con bonifico"));
    }

    [Fact]
    public void Validate_WithNullIBAN_AndBonifico_ReturnsWarning()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.Modalita = PaymentMethod.MP05_Bonifico;
        paymentInfo.IBAN = null;
        var (isValid, errors, warnings) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue("missing IBAN with bonifico is a warning, not an error");
        errors.Should().BeEmpty();
        warnings.Should().Contain(w => w.Contains("IBAN non specificato per pagamento con bonifico"));
    }

    #endregion

    #region Enum validation

    [Fact]
    public void Validate_WithInvalidCondizioni_ReturnsError()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.Condizioni = (PaymentCondition)99;
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeFalse();
        errors.Should().Contain("Condizioni di pagamento non valide");
    }

    [Fact]
    public void Validate_WithInvalidModalita_ReturnsError()
    {
        var paymentInfo = CreateValidPaymentInfo();
        paymentInfo.Modalita = (PaymentMethod)99;
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeFalse();
        errors.Should().Contain("Modalità di pagamento non valida");
    }

    #endregion

    #region Multiple payment methods without IBAN (no warning)

    [Theory]
    [InlineData(PaymentMethod.MP01_Contanti)]
    [InlineData(PaymentMethod.MP02_Assegno)]
    [InlineData(PaymentMethod.MP08_CartaDiPagamento)]
    [InlineData(PaymentMethod.MP12_RIBA)]
    [InlineData(PaymentMethod.MP23_PagoPA)]
    public void Validate_WithNonBonificoMethod_NoIBAN_NoWarning(PaymentMethod method)
    {
        var paymentInfo = new PaymentInfo
        {
            Condizioni = PaymentCondition.TP02_Completo,
            Modalita = method,
            IBAN = null
        };
        var (isValid, errors, warnings) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
        warnings.Should().BeEmpty();
    }

    #endregion

    #region Multiple errors

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var paymentInfo = new PaymentInfo
        {
            Condizioni = (PaymentCondition)99,
            Modalita = (PaymentMethod)99,
            IBAN = "INVALID"
        };
        var (isValid, errors, _) = PaymentInfoValidator.Validate(paymentInfo);
        isValid.Should().BeFalse();
        errors.Count.Should().Be(3);
    }

    #endregion
}
