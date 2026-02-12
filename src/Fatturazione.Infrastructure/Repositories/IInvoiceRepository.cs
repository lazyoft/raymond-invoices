using Fatturazione.Domain.Models;

namespace Fatturazione.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Invoice entity
/// </summary>
public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<IEnumerable<Invoice>> GetByClientIdAsync(Guid clientId);
    Task<string?> GetLastInvoiceNumberAsync();
    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice?> UpdateAsync(Invoice invoice);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
