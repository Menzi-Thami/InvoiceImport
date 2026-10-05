namespace InvoiceImporter.Domain
{
    public interface IInvoiceRepository
    {
        /// <summary>
        /// Returns which of <paramref name="invoiceNumbers"/> are already stored, compared
        /// case-insensitively like the database collation.
        /// </summary>
        Task<IReadOnlySet<string>> GetExistingInvoiceNumbersAsync(
            IReadOnlyCollection<string> invoiceNumbers, CancellationToken cancellationToken);

        void AddInvoice(InvoiceHeader invoice);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
