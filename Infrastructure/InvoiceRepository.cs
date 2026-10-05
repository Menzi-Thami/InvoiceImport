// InvoiceRepository.cs
using InvoiceImporter.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceImporter.Infrastructure
{
    public class InvoiceRepository : IInvoiceRepository
    {
        // Each number becomes a SQL parameter; SQL Server allows at most 2,100 per command.
        private const int LookupChunkSize = 1000;

        private readonly InvoiceDbContext _context;

        public InvoiceRepository(InvoiceDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlySet<string>> GetExistingInvoiceNumbersAsync(
            IReadOnlyCollection<string> invoiceNumbers, CancellationToken cancellationToken)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var chunk in invoiceNumbers.Chunk(LookupChunkSize))
            {
                // A List (not the array) so Contains binds to List<T>.Contains, which EF translates.
                var numbers = chunk.ToList();
                var found = await _context.InvoiceHeaders
                    .AsNoTracking()
                    .Where(h => numbers.Contains(h.InvoiceNumber))
                    .Select(h => h.InvoiceNumber)
                    .ToListAsync(cancellationToken);

                existing.UnionWith(found);
            }

            return existing;
        }

        public void AddInvoice(InvoiceHeader invoice)
        {
            _context.InvoiceHeaders.Add(invoice);
            _context.InvoiceLines.AddRange(invoice.Lines);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            _context.SaveChangesAsync(cancellationToken);
    }
}
