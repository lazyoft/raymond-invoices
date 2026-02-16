using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using FluentAssertions;

namespace Fatturazione.Domain.Tests.Services;

/// <summary>
/// Tests for RitenutaService
/// Covers all ritenuta scenarios per Art. 25 DPR 600/73 and Art. 25-bis DPR 600/73
/// </summary>
public class RitenutaServiceTests
{
    private readonly RitenutaService _sut;

    public RitenutaServiceTests()
    {
        _sut = new RitenutaService();
    }

    #region AppliesRitenuta

    [Fact]
    public void AppliesRitenuta_WhenClientSubjectToRitenutaIsTrue_ReturnsTrue()
    {
        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = true
        };

        // Act
        var result = _sut.AppliesRitenuta(client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AppliesRitenuta_WhenClientSubjectToRitenutaIsFalse_ReturnsFalse()
    {
        // Arrange
        var client = new Client
        {
            SubjectToRitenuta = false
        };

        // Act
        var result = _sut.AppliesRitenuta(client);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CalculateRitenuta (backward-compatible overload with flat percentage)

    [Fact]
    public void CalculateRitenuta_WithImponibile1000AndPercentage20_Returns200()
    {
        // Arrange
        decimal imponibile = 1000m;
        decimal percentage = 20m;

        // Act
        var result = _sut.CalculateRitenuta(imponibile, percentage);

        // Assert
        result.Should().Be(200m);
    }

    [Fact]
    public void CalculateRitenuta_WithImponibileZero_ReturnsZero()
    {
        // Arrange
        decimal imponibile = 0m;
        decimal percentage = 20m;

        // Act
        var result = _sut.CalculateRitenuta(imponibile, percentage);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateRitenuta_RoundsToTwoDecimals()
    {
        // Arrange
        decimal imponibile = 333.33m;
        decimal percentage = 20m;

        // Act
        var result = _sut.CalculateRitenuta(imponibile, percentage);

        // Assert
        result.Should().Be(66.67m);
    }

    #endregion

    #region CalculateRitenuta (client-based overload) - Core Scenarios

    [Fact]
    public void CalculateRitenuta_Professional_1000x100percentx20percent_Returns200()
    {
        // Art. 25 DPR 600/73 - Professionisti: 20% su 100% dell'imponibile
        // 1000 x (100/100) x (20/100) = 200.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 20m,
            RitenutaBaseCalcoloPercentuale = 100m,
            CausalePagamento = CausalePagamento.A,
            ClientType = ClientType.Professional
        };

        var result = _sut.CalculateRitenuta(1000m, client);

        result.Should().Be(200.00m);
    }

    [Fact]
    public void CalculateRitenuta_AgentWithoutEmployees_10000x50percentx23percent_Returns1150()
    {
        // Art. 25-bis DPR 600/73 - Agenti senza dipendenti: 23% su 50% delle provvigioni
        // 10000 x (50/100) x (23/100) = 1150.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 50m,
            CausalePagamento = CausalePagamento.R,
            ClientType = ClientType.Professional
        };

        var result = _sut.CalculateRitenuta(10000m, client);

        result.Should().Be(1150.00m);
    }

    [Fact]
    public void CalculateRitenuta_AgentWithEmployees_10000x20percentx23percent_Returns460()
    {
        // Art. 25-bis DPR 600/73 - Agenti con dipendenti: 23% su 20% delle provvigioni
        // 10000 x (20/100) x (23/100) = 460.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 20m,
            CausalePagamento = CausalePagamento.Q,
            ClientType = ClientType.Professional
        };

        var result = _sut.CalculateRitenuta(10000m, client);

        result.Should().Be(460.00m);
    }

    [Fact]
    public void CalculateRitenuta_NonResident_5000x100percentx30percent_Returns1500()
    {
        // Art. 25 DPR 600/73 - Non residenti: 30% su 100% a titolo definitivo
        // 5000 x (100/100) x (30/100) = 1500.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT02,
            RitenutaPercentage = 30m,
            RitenutaBaseCalcoloPercentuale = 100m,
            CausalePagamento = CausalePagamento.P,
            ClientType = ClientType.Professional
        };

        var result = _sut.CalculateRitenuta(5000m, client);

        result.Should().Be(1500.00m);
    }

    [Fact]
    public void CalculateRitenuta_OccasionalWorker_2000x100percentx20percent_Returns400()
    {
        // Art. 25 DPR 600/73 - Prestazioni occasionali: 20% su 100%
        // 2000 x (100/100) x (20/100) = 400.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 20m,
            RitenutaBaseCalcoloPercentuale = 100m,
            CausalePagamento = CausalePagamento.M,
            ClientType = ClientType.Professional
        };

        var result = _sut.CalculateRitenuta(2000m, client);

        result.Should().Be(400.00m);
    }

    #endregion

    #region CalculateRitenuta (client-based) - Edge Cases and Rounding

    [Fact]
    public void CalculateRitenuta_ClientNotSubjectToRitenuta_ReturnsZero()
    {
        // Companies and PA are not subject to ritenuta
        var client = new Client
        {
            SubjectToRitenuta = false,
            ClientType = ClientType.Company,
            RitenutaPercentage = 20m,
            RitenutaBaseCalcoloPercentuale = 100m
        };

        var result = _sut.CalculateRitenuta(5000m, client);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateRitenuta_ZeroImponibile_ReturnsZero()
    {
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 20m,
            RitenutaBaseCalcoloPercentuale = 100m
        };

        var result = _sut.CalculateRitenuta(0m, client);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateRitenuta_RoundsToTwoDecimals_AgentScenario()
    {
        // 333.33 x (50/100) x (23/100) = 333.33 x 0.5 x 0.23 = 38.33295
        // Rounded to 38.33
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 50m,
            CausalePagamento = CausalePagamento.R
        };

        var result = _sut.CalculateRitenuta(333.33m, client);

        result.Should().Be(38.33m);
    }

    [Fact]
    public void CalculateRitenuta_RoundsToTwoDecimals_ProfessionalScenario()
    {
        // 1234.56 x (100/100) x (20/100) = 1234.56 x 1 x 0.20 = 246.912
        // Rounded to 246.91
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 20m,
            RitenutaBaseCalcoloPercentuale = 100m,
            CausalePagamento = CausalePagamento.A
        };

        var result = _sut.CalculateRitenuta(1234.56m, client);

        result.Should().Be(246.91m);
    }

    [Fact]
    public void CalculateRitenuta_RoundsToTwoDecimals_NonResidentScenario()
    {
        // 777.77 x (100/100) x (30/100) = 777.77 x 1 x 0.30 = 233.331
        // Rounded to 233.33
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT02,
            RitenutaPercentage = 30m,
            RitenutaBaseCalcoloPercentuale = 100m,
            CausalePagamento = CausalePagamento.P
        };

        var result = _sut.CalculateRitenuta(777.77m, client);

        result.Should().Be(233.33m);
    }

    [Fact]
    public void CalculateRitenuta_RoundsToTwoDecimals_AgentWithEmployeesScenario()
    {
        // 9999.99 x (20/100) x (23/100) = 9999.99 x 0.20 x 0.23 = 459.99954
        // Rounded to 460.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 20m,
            CausalePagamento = CausalePagamento.Q
        };

        var result = _sut.CalculateRitenuta(9999.99m, client);

        result.Should().Be(460.00m);
    }

    [Fact]
    public void CalculateRitenuta_PublicAdministrationNotSubject_ReturnsZero()
    {
        // PA uses split payment, not ritenuta — they are mutually exclusive
        // Art. 17-ter DPR 633/72
        var client = new Client
        {
            SubjectToRitenuta = false,
            SubjectToSplitPayment = true,
            ClientType = ClientType.PublicAdministration,
            RitenutaPercentage = 0m,
            RitenutaBaseCalcoloPercentuale = 100m
        };

        var result = _sut.CalculateRitenuta(10000m, client);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateRitenuta_SmallAmount_Professional()
    {
        // 0.01 x (100/100) x (20/100) = 0.002 -> rounded to 0.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 20m,
            RitenutaBaseCalcoloPercentuale = 100m,
            CausalePagamento = CausalePagamento.A
        };

        var result = _sut.CalculateRitenuta(0.01m, client);

        result.Should().Be(0.00m);
    }

    [Fact]
    public void CalculateRitenuta_LargeAmount_AgentWithoutEmployees()
    {
        // 100000 x (50/100) x (23/100) = 11500.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            TipoRitenuta = TipoRitenuta.RT01,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 50m,
            CausalePagamento = CausalePagamento.R
        };

        var result = _sut.CalculateRitenuta(100000m, client);

        result.Should().Be(11500.00m);
    }

    #endregion

    #region GetStandardRate

    [Theory]
    [InlineData(ClientType.Professional, 20.0)]
    [InlineData(ClientType.Company, 0.0)]
    [InlineData(ClientType.PublicAdministration, 0.0)]
    public void GetStandardRate_ReturnsCorrectRateForClientType(ClientType clientType, decimal expectedRate)
    {
        // Act
        var result = _sut.GetStandardRate(clientType);

        // Assert
        result.Should().Be(expectedRate);
    }

    #endregion

    #region CalculateRitenuta (client-based) - Effective Rate Verification

    [Fact]
    public void CalculateRitenuta_AgentWithoutEmployees_EffectiveRateIs11Point50Percent()
    {
        // The effective rate for agents without employees is 11.50%
        // 23% x 50% = 11.50%
        // Verify: 10000 x 11.50% = 1150.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 50m
        };

        var result = _sut.CalculateRitenuta(10000m, client);
        var effectiveRate = result / 10000m * 100m;

        effectiveRate.Should().Be(11.50m);
    }

    [Fact]
    public void CalculateRitenuta_AgentWithEmployees_EffectiveRateIs4Point60Percent()
    {
        // The effective rate for agents with employees is 4.60%
        // 23% x 20% = 4.60%
        // Verify: 10000 x 4.60% = 460.00
        var client = new Client
        {
            SubjectToRitenuta = true,
            RitenutaPercentage = 23m,
            RitenutaBaseCalcoloPercentuale = 20m
        };

        var result = _sut.CalculateRitenuta(10000m, client);
        var effectiveRate = result / 10000m * 100m;

        effectiveRate.Should().Be(4.60m);
    }

    #endregion
}
