using System.Collections.Concurrent;
using Fatturazione.Domain.Models;

namespace Fatturazione.Infrastructure.Data;

/// <summary>
/// Thread-safe in-memory data store for demo purposes
/// </summary>
public class InMemoryDataStore
{
    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private readonly ConcurrentDictionary<Guid, Invoice> _invoices = new();

    public ConcurrentDictionary<Guid, Client> Clients => _clients;
    public ConcurrentDictionary<Guid, Invoice> Invoices => _invoices;
}
