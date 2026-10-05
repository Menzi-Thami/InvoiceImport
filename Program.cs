using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InvoiceImporter.Application;
using InvoiceImporter.Domain;
using InvoiceImporter.Domain.Services;
using InvoiceImporter.Infrastructure;

namespace InvoiceImporter
{
    /// <summary>
    /// Composition root: reads the file path, wires the dependencies, and runs the import.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(o => o.SingleLine = true)
                       .SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                Console.Write("Enter the file path of the CSV file: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    throw new ArgumentException("No file path was entered.");
                }

                string filePath = NormalisePath(input);

                using var dbContext = new InvoiceDbContext();

                var csvReader = new CsvReader();
                var repository = new InvoiceRepository(dbContext);
                var dateTimeParser = new DateTimeParser();
                var invoiceFactory = new InvoiceFactory(dateTimeParser);
                var dataImporter = new DataImporter(
                    csvReader, loggerFactory.CreateLogger<DataImporter>(), repository, invoiceFactory);

                await dataImporter.ImportData(filePath);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Unique index on InvoiceNumber: another run imported one of these invoices first.
                logger.LogError(ex, "Invoice import failed: an invoice in the file has already been imported; nothing was saved");
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invoice import failed");
                Environment.ExitCode = 1;
            }
        }

        // Only the surrounding quotes from Explorer's "Copy as path" need removing; the
        // separators are already correct (doubling them breaks \\server\share paths).
        internal static string NormalisePath(string input) =>
            input.Trim().Trim('"');
    }
}
