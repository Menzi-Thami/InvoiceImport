using System;
using System.IO;
using InvoiceImporter.Infrastructure;
using Shouldly;
using Xunit;

namespace InvoiceImporter.Tests
{
    public class CsvReaderTests
    {
        [Theory]
        [InlineData("H1,H2\r\nA,1\r\nB,2\r\n")]
        [InlineData("H1,H2\r\nA,1\r\nB,2")]
        [InlineData("H1,H2\nA,1\nB,2\n")]
        [InlineData("H1,H2\nA,1\nB,2")]
        [InlineData("H1,H2\r\nA,1\nB,2\r\n")]
        public void ReadCsv_ReturnsEachDataRow_WhateverTheLineEndings(string content)
        {
            var rows = Read(content);

            rows.Count.ShouldBe(2);
            rows[0].ShouldBe(new[] { "A", "1" });
            rows[1].ShouldBe(new[] { "B", "2" });
        }

        [Fact]
        public void ReadCsv_KeepsLineBreaksInsideQuotedFields()
        {
            var rows = Read("H1,H2\r\n\"1 High St\r\nLeeds\",1\r\nB,2\r\n");

            rows.Count.ShouldBe(2);
            rows[0].ShouldBe(new[] { "1 High St\nLeeds", "1" });
            rows[1].ShouldBe(new[] { "B", "2" });
        }

        private static System.Collections.Generic.List<string[]> Read(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), $"csvreader-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, content);
            try
            {
                return new CsvReader().ReadCsv(path);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
