using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceImporter.Application;
using InvoiceImporter.Domain;
using InvoiceImporter.Domain.Services;
using InvoiceImporter.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace InvoiceImporter.Tests
{
    public class DataImporterTests
    {
        [Fact]
        public async Task ImportData_ImportsEveryRowTheReaderReturns()
        {
            var csv = new FakeCsvReader(new List<string[]>
            {
                Row("INV-001"),
                Row("INV-002"),
            });
            var repo = new FakeRepository();

            var importer = new DataImporter(csv, NullLogger<DataImporter>.Instance, repo, new PassthroughFactory());

            await importer.ImportData("any.csv", CancellationToken.None);

            repo.Added.Count.ShouldBe(2);
            repo.SaveChangesCallCount.ShouldBe(1);
        }

        [Fact]
        public async Task ImportData_SkipsInvoicesThatAlreadyExist()
        {
            var csv = new FakeCsvReader(new List<string[]>
            {
                Row("INV-001"),
                Row("INV-002"),
            });
            var repo = new FakeRepository();
            repo.Existing.Add("INV-001");

            var importer = new DataImporter(csv, NullLogger<DataImporter>.Instance, repo, new PassthroughFactory());

            await importer.ImportData("any.csv", CancellationToken.None);

            repo.Added.Count.ShouldBe(1);
            repo.Added[0].InvoiceNumber.ShouldBe("INV-002");
        }

        [Fact]
        public async Task ImportData_SkipsDuplicateInvoiceNumbersWithinOneFile()
        {
            var csv = new FakeCsvReader(new List<string[]>
            {
                Row("INV-001"),
                Row("INV-001"),
                Row("inv-001"),
                Row("INV-002"),
            });
            var repo = new FakeRepository();

            var importer = new DataImporter(csv, NullLogger<DataImporter>.Instance, repo, new PassthroughFactory());

            await importer.ImportData("any.csv", CancellationToken.None);

            repo.Added.Count.ShouldBe(2);
            repo.Added[0].InvoiceNumber.ShouldBe("INV-001");
            repo.Added[1].InvoiceNumber.ShouldBe("INV-002");
        }

        [Fact]
        public async Task ImportData_LooksUpExistingInvoicesOnce_RegardlessOfRowCount()
        {
            var rows = new List<string[]>();
            rows.AddRange(Enumerable.Range(1, 50).Select(i => Row($"INV-{i:000}")));
            rows.Add(Row("INV-001")); // repeat: requested once, not twice
            var repo = new FakeRepository();

            var importer = new DataImporter(new FakeCsvReader(rows), NullLogger<DataImporter>.Instance, repo, new PassthroughFactory());

            await importer.ImportData("any.csv", CancellationToken.None);

            repo.LookupCallCount.ShouldBe(1);
            repo.LastRequested.Count.ShouldBe(50);
            repo.Added.Count.ShouldBe(50);
        }

        [Fact]
        public async Task ImportData_WhenCancelled_SavesNothing()
        {
            var csv = new FakeCsvReader(new List<string[]> { Row("INV-001") });
            var repo = new FakeRepository();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var importer = new DataImporter(csv, NullLogger<DataImporter>.Instance, repo, new PassthroughFactory());

            await Should.ThrowAsync<OperationCanceledException>(() => importer.ImportData("any.csv", cts.Token));
            repo.SaveChangesCallCount.ShouldBe(0);
        }

        [Fact]
        public async Task ImportData_PropagatesReaderFailures_InsteadOfSwallowing()
        {
            var csv = new ThrowingCsvReader();

            var importer = new DataImporter(csv, NullLogger<DataImporter>.Instance, new FakeRepository(), new PassthroughFactory());

            await Should.ThrowAsync<InvalidOperationException>(() => importer.ImportData("any.csv", CancellationToken.None));
        }

        [Fact]
        public async Task ImportData_FromFileWithHeader_ImportsEveryInvoice()
        {
            var path = WriteCsv(
                "InvoiceNumber,InvoiceDate,Address,InvoiceTotal,Description,Quantity,UnitPrice",
                "INV-001,07/04/2024 14:30,1 High Street,10,Widget,1,10",
                "INV-002,08/04/2024 09:00,2 Low Road,25.5,Gadget,3,8.5");
            try
            {
                var repo = new FakeRepository();
                var importer = new DataImporter(
                    new CsvReader(), NullLogger<DataImporter>.Instance, repo, new InvoiceFactory(new DateTimeParser()));

                await importer.ImportData(path, CancellationToken.None);

                repo.Added.Select(i => i.InvoiceNumber).ShouldBe(new[] { "INV-001", "INV-002" });
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task ImportData_FromHeaderOnlyFile_ImportsNothingWithoutError()
        {
            var path = WriteCsv("InvoiceNumber,InvoiceDate,Address,InvoiceTotal,Description,Quantity,UnitPrice");
            try
            {
                var repo = new FakeRepository();
                var importer = new DataImporter(
                    new CsvReader(), NullLogger<DataImporter>.Instance, repo, new InvoiceFactory(new DateTimeParser()));

                await importer.ImportData(path, CancellationToken.None);

                repo.Added.ShouldBeEmpty();
                repo.SaveChangesCallCount.ShouldBe(1);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string WriteCsv(params string[] lines)
        {
            // CRLF explicitly: CSVFile's default LineSeparator is "\r\n", and WriteAllLines would use LF on Linux CI.
            var path = Path.Combine(Path.GetTempPath(), $"invoices-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
            return path;
        }

        private static string[] Row(string invoiceNumber) =>
            new[] { invoiceNumber, "07/04/2024 14:30", "Addr", "10", "Widget", "1", "10" };

        private sealed class FakeCsvReader : ICsvReader
        {
            private readonly List<string[]> _rows;
            public FakeCsvReader(List<string[]> rows) => _rows = rows;
            public List<string[]> ReadCsv(string filePath) => _rows;
        }

        private sealed class ThrowingCsvReader : ICsvReader
        {
            public List<string[]> ReadCsv(string filePath) =>
                throw new InvalidOperationException("boom");
        }

        private sealed class PassthroughFactory : IInvoiceFactory
        {
            public InvoiceHeader CreateInvoice(string[] csvRow) =>
                new(csvRow[0], new DateTime(2024, 4, 7), csvRow[2], 10);
        }

        private sealed class FakeRepository : IInvoiceRepository
        {
            public HashSet<string> Existing { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<InvoiceHeader> Added { get; } = new();
            public int SaveChangesCallCount { get; private set; }
            public int LookupCallCount { get; private set; }
            public IReadOnlyCollection<string> LastRequested { get; private set; } = Array.Empty<string>();

            public Task<IReadOnlySet<string>> GetExistingInvoiceNumbersAsync(
                IReadOnlyCollection<string> invoiceNumbers, CancellationToken cancellationToken)
            {
                LookupCallCount++;
                LastRequested = invoiceNumbers;
                IReadOnlySet<string> found = new HashSet<string>(
                    invoiceNumbers.Where(Existing.Contains), StringComparer.OrdinalIgnoreCase);
                return Task.FromResult(found);
            }

            public void AddInvoice(InvoiceHeader invoice) => Added.Add(invoice);

            public Task SaveChangesAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveChangesCallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
