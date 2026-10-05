using System;
using System.Globalization;

namespace InvoiceImporter.Domain.Services
{
    /// <summary>
    /// Parses invoice dates with exactly one format. Guessing per value (UK first, then US)
    /// silently swaps day and month for any date with a day of 12 or less.
    /// </summary>
    public class DateTimeParser : IDateTimeParser
    {
        public const string UkFormat = "dd/MM/yyyy HH:mm";
        public const string UsFormat = "MM/dd/yyyy HH:mm";

        private readonly string _format;

        public DateTimeParser(string format = UkFormat)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("A date format is required.", nameof(format));
            }

            _format = format;
        }

        public DateTime ParseDateTime(string dateTimeString)
        {
            if (DateTime.TryParseExact(dateTimeString, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invoiceDate))
            {
                return invoiceDate;
            }

            throw new ArgumentException($"Unable to parse the date string '{dateTimeString}': expected the format {_format}.");
        }
    }
}
