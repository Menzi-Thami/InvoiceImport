using System;
using InvoiceImporter.Domain;
using InvoiceImporter.Domain.Services;
using Shouldly;
using Xunit;

namespace InvoiceImporter.Tests
{
    public class InvoiceFactoryTests
    {
        private static readonly DateTime FixedDate = new(2024, 4, 7, 14, 30, 0);

        private readonly InvoiceFactory _factory = new(new StubDateTimeParser(FixedDate));

        [Fact]
        public void CreateInvoice_MapsHeaderFields()
        {
            var row = new[] { "INV-001", "07/04/2024 14:30", "1 High St", "120.50", "Widget", "2", "60.25" };

            var invoice = _factory.CreateInvoice(row);

            invoice.InvoiceNumber.ShouldBe("INV-001");
            invoice.InvoiceDate.ShouldBe(FixedDate);
            invoice.Address.ShouldBe("1 High St");
            invoice.InvoiceTotal.ShouldBe(120.50);
        }

        [Fact]
        public void CreateInvoice_MapsSingleLine()
        {
            var row = new[] { "INV-001", "07/04/2024 14:30", "1 High St", "120.50", "Widget", "2", "60.25" };

            var invoice = _factory.CreateInvoice(row);

            invoice.Lines.Count.ShouldBe(1);
            var line = invoice.Lines[0];
            line.Description.ShouldBe("Widget");
            line.Quantity.ShouldBe(2);
            line.UnitSellingPriceExVAT.ShouldBe(60.25);
        }

        [Fact]
        public void CreateInvoice_MapsMultipleLines()
        {
            var row = new[]
            {
                "INV-002", "07/04/2024 14:30", "2 Low St", "300",
                "Widget", "2", "60.25",
                "Gadget", "1", "40"
            };

            var invoice = _factory.CreateInvoice(row);

            invoice.Lines.Count.ShouldBe(2);
            invoice.Lines[1].Description.ShouldBe("Gadget");
            invoice.Lines[1].Quantity.ShouldBe(1);
        }

        [Fact]
        public void CreateInvoice_IgnoresTrailingPartialLine()
        {
            // Two extra columns is not a full 3-column line and must not throw.
            var row = new[]
            {
                "INV-003", "07/04/2024 14:30", "3 Mid St", "60",
                "Widget", "2", "60.25",
                "Gadget", "1" // incomplete
            };

            var invoice = _factory.CreateInvoice(row);

            invoice.Lines.Count.ShouldBe(1);
        }

        [Theory]
        [InlineData("1,5")]      // decimal comma: NumberStyles.Any read this as 15
        [InlineData("(10)")]     // accounting negative: NumberStyles.Any read this as -10
        [InlineData("1.234,56")]
        [InlineData("n/a")]
        public void CreateInvoice_ThrowsOnUnreadableTotal_NamingInvoiceAndColumn(string total)
        {
            var row = new[] { "INV-004", "07/04/2024 14:30", "4 Top St", total, "Widget", "1", "10" };

            var ex = Should.Throw<FormatException>(() => _factory.CreateInvoice(row));

            ex.Message.ShouldContain("INV-004");
            ex.Message.ShouldContain("InvoiceTotal");
        }

        [Theory]
        [InlineData("-", "10", "Quantity")]
        [InlineData("1", "abc", "UnitSellingPriceExVAT")]
        public void CreateInvoice_ThrowsOnUnreadableLineAmount_NamingColumn(string quantity, string unitPrice, string column)
        {
            var row = new[] { "INV-004", "07/04/2024 14:30", "4 Top St", "10", "Widget", quantity, unitPrice };

            var ex = Should.Throw<FormatException>(() => _factory.CreateInvoice(row));

            ex.Message.ShouldContain("INV-004");
            ex.Message.ShouldContain(column);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public void CreateInvoice_MapsBlankAmountsToNull(string blank)
        {
            var row = new[] { "INV-004", "07/04/2024 14:30", "4 Top St", blank, "Widget", blank, blank };

            var invoice = _factory.CreateInvoice(row);

            invoice.InvoiceTotal.ShouldBeNull();
            invoice.Lines[0].Quantity.ShouldBeNull();
            invoice.Lines[0].UnitSellingPriceExVAT.ShouldBeNull();
        }

        [Theory]
        [InlineData("1.5", 1.5)]
        [InlineData(" -2.25 ", -2.25)]
        [InlineData("10", 10)]
        public void CreateInvoice_ParsesInvariantAmounts(string total, double expected)
        {
            var row = new[] { "INV-004", "07/04/2024 14:30", "4 Top St", total, "Widget", "1", "10" };

            _factory.CreateInvoice(row).InvoiceTotal.ShouldBe(expected);
        }

        [Fact]
        public void CreateInvoice_ThrowsWhenRowIsNull()
        {
            Should.Throw<ArgumentException>(() => _factory.CreateInvoice(null!));
        }

        [Fact]
        public void CreateInvoice_ThrowsWhenTooFewColumns()
        {
            var row = new[] { "INV-005", "07/04/2024 14:30", "5 Any St", "10" };

            Should.Throw<ArgumentException>(() => _factory.CreateInvoice(row));
        }

        [Fact]
        public void CreateInvoice_ThrowsWhenInvoiceNumberIsBlank()
        {
            var row = new[] { "  ", "07/04/2024 14:30", "6 Any St", "10", "Widget", "1", "10" };

            Should.Throw<ArgumentException>(() => _factory.CreateInvoice(row));
        }

        private sealed class StubDateTimeParser : IDateTimeParser
        {
            private readonly DateTime _value;

            public StubDateTimeParser(DateTime value) => _value = value;

            public DateTime ParseDateTime(string dateTimeString) => _value;
        }
    }
}
