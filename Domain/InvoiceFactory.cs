using System;
using System.Globalization;
using InvoiceImporter.Domain.Services;

namespace InvoiceImporter.Domain
{
    public class InvoiceFactory : IInvoiceFactory
    {
        private const int MinimumColumns = 7;
        private const int FirstLineColumn = 4;
        private const int ColumnsPerLine = 3;

        private readonly IDateTimeParser _dateTimeParser;

        public InvoiceFactory(IDateTimeParser dateTimeParser)
        {
            _dateTimeParser = dateTimeParser;
        }

        public InvoiceHeader CreateInvoice(string[] csvRow)
        {
            if (csvRow == null || csvRow.Length < MinimumColumns)
            {
                throw new ArgumentException(
                    $"Invalid CSV row format: expected at least {MinimumColumns} columns.",
                    nameof(csvRow));
            }

            var invoiceDate = _dateTimeParser.ParseDateTime(csvRow[1]);

            var invoice = new InvoiceHeader(
                invoiceNumber: csvRow[0],
                invoiceDate: invoiceDate,
                address: csvRow[2],
                invoiceTotal: ParseAmount(csvRow[3], csvRow[0], nameof(InvoiceHeader.InvoiceTotal)));

            // Each line occupies three columns: Description, Quantity, UnitPrice.
            for (int i = FirstLineColumn, lineNumber = 1; i + ColumnsPerLine - 1 < csvRow.Length; i += ColumnsPerLine, lineNumber++)
            {
                invoice.AddLine(new InvoiceLine(
                    description: csvRow[i],
                    quantity: ParseAmount(csvRow[i + 1], csvRow[0], $"line {lineNumber} {nameof(InvoiceLine.Quantity)}"),
                    unitSellingPriceExVat: ParseAmount(csvRow[i + 2], csvRow[0], $"line {lineNumber} {nameof(InvoiceLine.UnitSellingPriceExVAT)}")));
            }

            return invoice;
        }

        // No thousands separators, currency symbols or parentheses: "1,5" must not become 15.
        private const NumberStyles AmountStyle =
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        private static double? ParseAmount(string value, string invoiceNumber, string column)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (double.TryParse(value, AmountStyle, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new FormatException(
                $"Invoice {invoiceNumber}: {column} value '{value}' is not a number in the form 1234.56.");
        }
    }
}
