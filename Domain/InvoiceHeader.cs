using System;
using System.Collections.Generic;

namespace InvoiceImporter.Domain
{
    /// <summary>
    /// Invoice aggregate root. Plain C# domain type — no EF or CSV concerns.
    /// Persistence mapping lives in Infrastructure (Fluent API); parsing lives in the factory.
    /// </summary>
    public class InvoiceHeader
    {
        // Parameterless constructor for the EF Core materialiser only.
        private InvoiceHeader()
        {
            InvoiceNumber = string.Empty;
            Address = string.Empty;
        }

        public InvoiceHeader(string invoiceNumber, DateTime? invoiceDate, string address, double? invoiceTotal)
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));
            }

            InvoiceNumber = invoiceNumber;
            InvoiceDate = invoiceDate;
            Address = address ?? string.Empty;
            InvoiceTotal = invoiceTotal;
        }

        public int InvoiceId { get; private set; }
        public string InvoiceNumber { get; private set; }
        public DateTime? InvoiceDate { get; private set; }
        public string Address { get; private set; }
        public double? InvoiceTotal { get; private set; }

        private readonly List<InvoiceLine> _lines = new();
        public IReadOnlyList<InvoiceLine> Lines => _lines;

        public void AddLine(InvoiceLine line)
        {
            ArgumentNullException.ThrowIfNull(line);

            _lines.Add(line);
        }
    }
}
