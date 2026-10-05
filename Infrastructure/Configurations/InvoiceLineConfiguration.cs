using InvoiceImporter.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceImporter.Infrastructure.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="InvoiceLine"/>.
    /// </summary>
    public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("InvoiceLines");

            builder.HasKey(e => e.LineId);
            builder.Property(e => e.LineId).ValueGeneratedOnAdd();

            builder.Property(e => e.Description);
            builder.Property(e => e.Quantity);
            builder.Property(e => e.UnitSellingPriceExVAT);
        }
    }
}
