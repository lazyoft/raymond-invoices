using Fatturazione.Domain.Models;
using Fatturazione.Infrastructure.Data;

namespace Fatturazione.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of Invoice repository
/// </summary>
public class InvoiceRepository : IInvoiceRepository
{
    private readonly InMemoryDataStore _dataStore;

    public InvoiceRepository(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<IEnumerable<Invoice>> GetAllAsync()
    {
        var invoices = _dataStore.Invoices.Values
            .OrderByDescending(i => i.InvoiceDate)
            .AsEnumerable();
        return Task.FromResult(invoices);
    }

    public Task<Invoice?> GetByIdAsync(Guid id)
    {
        _dataStore.Invoices.TryGetValue(id, out var invoice);

        // Load client if needed
        if (invoice != null && invoice.ClientId != Guid.Empty)
        {
            _dataStore.Clients.TryGetValue(invoice.ClientId, out var client);
            invoice.Client = client;
        }

        return Task.FromResult(invoice);
    }

    public Task<IEnumerable<Invoice>> GetByClientIdAsync(Guid clientId)
    {
        var invoices = _dataStore.Invoices.Values
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.InvoiceDate)
            .AsEnumerable();

        // Load client for each invoice
        foreach (var invoice in invoices)
        {
            if (_dataStore.Clients.TryGetValue(invoice.ClientId, out var client))
            {
                invoice.Client = client;
            }
        }

        return Task.FromResult(invoices);
    }

    public Task<string?> GetLastInvoiceNumberAsync()
    {
        var lastInvoice = _dataStore.Invoices.Values
            .Where(i => !string.IsNullOrEmpty(i.InvoiceNumber))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefault();

        return Task.FromResult(lastInvoice?.InvoiceNumber);
    }

    public Task<Invoice> CreateAsync(Invoice invoice)
    {
        if (invoice.Id == Guid.Empty)
            invoice.Id = Guid.NewGuid();

        invoice.CreatedAt = DateTime.UtcNow;

        _dataStore.Invoices.TryAdd(invoice.Id, invoice);
        return Task.FromResult(invoice);
    }

    public Task<Invoice?> UpdateAsync(Invoice invoice)
    {
        if (!_dataStore.Invoices.ContainsKey(invoice.Id))
            return Task.FromResult<Invoice?>(null);

        invoice.ModifiedAt = DateTime.UtcNow;
        _dataStore.Invoices[invoice.Id] = invoice;
        return Task.FromResult<Invoice?>(invoice);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var removed = _dataStore.Invoices.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        var exists = _dataStore.Invoices.ContainsKey(id);
        return Task.FromResult(exists);
    }
}
