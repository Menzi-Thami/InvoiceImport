using Microsoft.Extensions.Logging;
using InvoiceImporter.Domain;

namespace InvoiceImporter.Application
{
    public class DataImporter : IDataImporter
    {
        private readonly ICsvReader _csvReader;
        private readonly ILogger<DataImporter> _logger;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IInvoiceFactory _invoiceFactory;

        public DataImporter(
            ICsvReader csvReader,
            ILogger<DataImporter> logger,
            IInvoiceRepository invoiceRepository,
            IInvoiceFactory invoiceFactory)
        {
            _csvReader = csvReader;
            _logger = logger;
            _invoiceRepository = invoiceRepository;
            _invoiceFactory = invoiceFactory;
        }

        public async Task ImportData(string filePath, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reading CSV file {FilePath}", filePath);

            List<string[]> rows = _csvReader.ReadCsv(filePath);

            // Case-insensitive to match the database's default collation, which the
            // unique index on InvoiceNumber enforces: "inv-001" and "INV-001" collide there.
            var fileNumbers = rows.Select(r => r[0]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existing = await _invoiceRepository.GetExistingInvoiceNumbersAsync(fileNumbers, cancellationToken);

            int imported = 0, skipped = 0;
            var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var invoiceNumber = row[0];
                if (queued.Contains(invoiceNumber))
                {
                    _logger.LogWarning("Invoice {InvoiceNumber} appears more than once in the file; skipping the repeat", invoiceNumber);
                    skipped++;
                    continue;
                }

                if (existing.Contains(invoiceNumber))
                {
                    _logger.LogWarning("Invoice {InvoiceNumber} already exists; skipping", invoiceNumber);
                    skipped++;
                    continue;
                }

                var invoice = _invoiceFactory.CreateInvoice(row);
                _invoiceRepository.AddInvoice(invoice);
                queued.Add(invoiceNumber);
                imported++;
                _logger.LogDebug("Invoice {InvoiceNumber} queued for import", invoiceNumber);
            }

            await _invoiceRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Import completed: {Imported} imported, {Skipped} skipped from {FilePath}",
                imported, skipped, filePath);
        }
    }
}
