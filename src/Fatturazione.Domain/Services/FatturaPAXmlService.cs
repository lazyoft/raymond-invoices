using System.Globalization;
using System.Xml.Linq;
using Fatturazione.Domain.Models;

namespace Fatturazione.Domain.Services;

/// <summary>
/// Generates FatturaPA XML (tracciato v1.2.2) from an Invoice and IssuerProfile.
/// Conforme a DL 119/2018, DL 127/2015, Provvedimento AdE 89757/2018.
/// </summary>
public class FatturaPAXmlService : IFatturaPAXmlService
{
    private static readonly XNamespace Ns = "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2";

    /// <inheritdoc />
    public string GenerateXml(Invoice invoice, IssuerProfile issuer)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(issuer);

        if (invoice.Client is null)
            throw new ArgumentException("Invoice must have a Client navigation property populated.", nameof(invoice));

        var client = invoice.Client;
        var formatoTrasmissione = client.ClientType == ClientType.PublicAdministration ? "FPA12" : "FPR12";

        var root = new XElement(Ns + "FatturaElettronica",
            new XAttribute("versione", formatoTrasmissione),
            BuildHeader(invoice, issuer, client, formatoTrasmissione),
            BuildBody(invoice, client));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);

        using var writer = new StringWriter();
        doc.Save(writer);
        return writer.ToString();
    }

    #region Header

    private static XElement BuildHeader(Invoice invoice, IssuerProfile issuer, Client client, string formatoTrasmissione)
    {
        return new XElement("FatturaElettronicaHeader",
            BuildDatiTrasmissione(invoice, issuer, client, formatoTrasmissione),
            BuildCedentePrestatore(issuer),
            BuildCessionarioCommittente(client));
    }

    private static XElement BuildDatiTrasmissione(Invoice invoice, IssuerProfile issuer, Client client, string formatoTrasmissione)
    {
        var codiceDestinatario = DetermineCodiceDestinatario(client);
        var progressivoInvio = invoice.InvoiceNumber.Replace("/", "", StringComparison.Ordinal);

        var datiTrasmissione = new XElement("DatiTrasmissione",
            new XElement("IdTrasmittente",
                new XElement("IdPaese", "IT"),
                new XElement("IdCodice", issuer.PartitaIva)),
            new XElement("ProgressivoInvio", progressivoInvio),
            new XElement("FormatoTrasmissione", formatoTrasmissione),
            new XElement("CodiceDestinatario", codiceDestinatario));

        // PEC is used when CodiceDestinatario is "0000000" and PEC is available
        if (codiceDestinatario == "0000000" && !string.IsNullOrWhiteSpace(client.PEC))
        {
            datiTrasmissione.Add(new XElement("PECDestinatario", client.PEC));
        }

        return datiTrasmissione;
    }

    private static string DetermineCodiceDestinatario(Client client)
    {
        // PA clients use CodiceUnivocoUfficio (6 characters)
        if (client.ClientType == ClientType.PublicAdministration &&
            !string.IsNullOrWhiteSpace(client.CodiceUnivocoUfficio))
        {
            return client.CodiceUnivocoUfficio;
        }

        // B2B clients with SDI code
        if (!string.IsNullOrWhiteSpace(client.CodiceDestinatario))
        {
            return client.CodiceDestinatario;
        }

        // Default: "0000000" (PEC or cassetto fiscale)
        return "0000000";
    }

    private static XElement BuildCedentePrestatore(IssuerProfile issuer)
    {
        return new XElement("CedentePrestatore",
            new XElement("DatiAnagrafici",
                new XElement("IdFiscaleIVA",
                    new XElement("IdPaese", "IT"),
                    new XElement("IdCodice", issuer.PartitaIva)),
                new XElement("Anagrafica",
                    new XElement("Denominazione", issuer.RagioneSociale)),
                new XElement("RegimeFiscale", issuer.RegimeFiscale)),
            new XElement("Sede",
                new XElement("Indirizzo", issuer.Indirizzo.Street),
                new XElement("CAP", issuer.Indirizzo.PostalCode),
                new XElement("Comune", issuer.Indirizzo.City),
                new XElement("Provincia", issuer.Indirizzo.Province),
                new XElement("Nazione", "IT")));
    }

    private static XElement BuildCessionarioCommittente(Client client)
    {
        var datiAnagrafici = new XElement("DatiAnagrafici");

        // IdFiscaleIVA if client has Partita IVA
        if (!string.IsNullOrWhiteSpace(client.PartitaIva))
        {
            datiAnagrafici.Add(new XElement("IdFiscaleIVA",
                new XElement("IdPaese", "IT"),
                new XElement("IdCodice", client.PartitaIva)));
        }

        // CodiceFiscale if available
        if (!string.IsNullOrWhiteSpace(client.CodiceFiscale))
        {
            datiAnagrafici.Add(new XElement("CodiceFiscale", client.CodiceFiscale));
        }

        datiAnagrafici.Add(new XElement("Anagrafica",
            new XElement("Denominazione", client.RagioneSociale)));

        return new XElement("CessionarioCommittente",
            datiAnagrafici,
            new XElement("Sede",
                new XElement("Indirizzo", client.Address.Street),
                new XElement("CAP", client.Address.PostalCode),
                new XElement("Comune", client.Address.City),
                new XElement("Provincia", client.Address.Province),
                new XElement("Nazione", "IT")));
    }

    #endregion

    #region Body

    private static XElement BuildBody(Invoice invoice, Client client)
    {
        var body = new XElement("FatturaElettronicaBody",
            BuildDatiGenerali(invoice, client),
            BuildDatiBeniServizi(invoice));

        // DatiPagamento (blocco 2.4) — only when PaymentInfo is present
        if (invoice.PaymentInfo is not null)
        {
            body.Add(BuildDatiPagamento(invoice));
        }

        return body;
    }

    private static XElement BuildDatiGenerali(Invoice invoice, Client client)
    {
        var datiGenerali = new XElement("DatiGenerali",
            BuildDatiGeneraliDocumento(invoice, client));

        // DatiFattureCollegate for credit/debit notes (TD04, TD05)
        if ((invoice.DocumentType == DocumentType.TD04 || invoice.DocumentType == DocumentType.TD05) &&
            !string.IsNullOrWhiteSpace(invoice.RelatedInvoiceNumber))
        {
            datiGenerali.Add(new XElement("DatiFattureCollegate",
                new XElement("IdDocumento", invoice.RelatedInvoiceNumber)));
        }

        return datiGenerali;
    }

    private static XElement BuildDatiGeneraliDocumento(Invoice invoice, Client client)
    {
        var doc = new XElement("DatiGeneraliDocumento",
            new XElement("TipoDocumento", invoice.DocumentType.ToString()),
            new XElement("Divisa", "EUR"),
            new XElement("Data", invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement("Numero", invoice.InvoiceNumber));

        // DatiRitenuta — when the client is subject to ritenuta and ritenuta amount is set
        if (client.SubjectToRitenuta && invoice.RitenutaAmount != 0)
        {
            var causalePagamento = client.CausalePagamento?.ToString() ?? "A";
            doc.Add(new XElement("DatiRitenuta",
                new XElement("TipoRitenuta", client.TipoRitenuta.ToString()),
                new XElement("ImportoRitenuta", FormatDecimal(Math.Abs(invoice.RitenutaAmount))),
                new XElement("AliquotaRitenuta", FormatDecimal(client.RitenutaPercentage)),
                new XElement("CausalePagamento", causalePagamento)));
        }

        // BolloVirtuale — Art. 13 DPR 642/72
        if (invoice.BolloVirtuale)
        {
            doc.Add(new XElement("DatiBollo",
                new XElement("BolloVirtuale", "SI"),
                new XElement("ImportoBollo", FormatDecimal(invoice.BolloAmount))));
        }

        // Causale
        if (!string.IsNullOrWhiteSpace(invoice.Causale))
        {
            doc.Add(new XElement("Causale", invoice.Causale));
        }

        return doc;
    }

    private static XElement BuildDatiBeniServizi(Invoice invoice)
    {
        var datiBeniServizi = new XElement("DatiBeniServizi");

        // DettaglioLinee — one per invoice item
        var lineNumber = 1;
        foreach (var item in invoice.Items)
        {
            var dettaglio = new XElement("DettaglioLinee",
                new XElement("NumeroLinea", lineNumber),
                new XElement("Descrizione", item.Description),
                new XElement("Quantita", FormatDecimal(item.Quantity)),
                new XElement("PrezzoUnitario", FormatDecimal(item.UnitPrice)),
                new XElement("PrezzoTotale", FormatDecimal(item.Imponibile)),
                new XElement("AliquotaIVA", FormatDecimal((int)item.IvaRate)));

            // Natura — required when AliquotaIVA is 0 (IvaRate.Zero)
            if (item.IvaRate == IvaRate.Zero && item.NaturaIva.HasValue)
            {
                dettaglio.Add(new XElement("Natura", FormatNaturaIva(item.NaturaIva.Value)));
            }

            datiBeniServizi.Add(dettaglio);
            lineNumber++;
        }

        // DatiRiepilogo — one per IVA rate group
        var rateGroups = invoice.Items
            .GroupBy(i => new { i.IvaRate, i.NaturaIva })
            .ToList();

        foreach (var group in rateGroups)
        {
            var sumImponibile = group.Sum(i => i.Imponibile);
            var sumIva = group.Sum(i => i.IvaAmount);

            var riepilogo = new XElement("DatiRiepilogo",
                new XElement("AliquotaIVA", FormatDecimal((int)group.Key.IvaRate)),
                new XElement("ImponibileImporto", FormatDecimal(sumImponibile)),
                new XElement("Imposta", FormatDecimal(sumIva)),
                new XElement("EsigibilitaIVA", FormatEsigibilitaIva(invoice.EsigibilitaIva)));

            // Natura for zero-rate groups
            if (group.Key.IvaRate == IvaRate.Zero && group.Key.NaturaIva.HasValue)
            {
                riepilogo.Add(new XElement("Natura", FormatNaturaIva(group.Key.NaturaIva.Value)));
            }

            datiBeniServizi.Add(riepilogo);
        }

        return datiBeniServizi;
    }

    private static XElement BuildDatiPagamento(Invoice invoice)
    {
        var paymentInfo = invoice.PaymentInfo!;

        var dettaglioPagamento = new XElement("DettaglioPagamento",
            new XElement("ModalitaPagamento", FormatPaymentMethod(paymentInfo.Modalita)),
            new XElement("DataScadenzaPagamento", invoice.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement("ImportoPagamento", FormatDecimal(invoice.TotalDue)));

        if (!string.IsNullOrWhiteSpace(paymentInfo.IBAN))
        {
            dettaglioPagamento.Add(new XElement("IBAN", paymentInfo.IBAN));
        }

        return new XElement("DatiPagamento",
            new XElement("CondizioniPagamento", FormatPaymentCondition(paymentInfo.Condizioni)),
            dettaglioPagamento);
    }

    #endregion

    #region Formatting Helpers

    /// <summary>
    /// Formats a decimal to 2 decimal places with invariant culture (dot separator).
    /// </summary>
    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts NaturaIva enum to FatturaPA format: N2_1 → "N2.1", N6_3 → "N6.3", N1 → "N1"
    /// </summary>
    public static string FormatNaturaIva(NaturaIva natura)
    {
        return natura.ToString().Replace("_", ".");
    }

    /// <summary>
    /// Converts EsigibilitaIva enum to FatturaPA code: Immediata → "I", Differita → "D", SplitPayment → "S"
    /// </summary>
    public static string FormatEsigibilitaIva(EsigibilitaIva esigibilita)
    {
        return esigibilita switch
        {
            EsigibilitaIva.Immediata => "I",
            EsigibilitaIva.Differita => "D",
            EsigibilitaIva.SplitPayment => "S",
            _ => "I"
        };
    }

    /// <summary>
    /// Extracts the FatturaPA code from PaymentMethod enum: MP05_Bonifico → "MP05"
    /// </summary>
    public static string FormatPaymentMethod(PaymentMethod method)
    {
        var name = method.ToString();
        var underscoreIndex = name.IndexOf('_');
        return underscoreIndex > 0 ? name[..underscoreIndex] : name;
    }

    /// <summary>
    /// Extracts the FatturaPA code from PaymentCondition enum: TP02_Completo → "TP02"
    /// </summary>
    public static string FormatPaymentCondition(PaymentCondition condition)
    {
        var name = condition.ToString();
        var underscoreIndex = name.IndexOf('_');
        return underscoreIndex > 0 ? name[..underscoreIndex] : name;
    }

    #endregion
}
