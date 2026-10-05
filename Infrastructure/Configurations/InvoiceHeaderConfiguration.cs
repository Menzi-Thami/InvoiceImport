using InvoiceImporter.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceImporter.Infrastructure.Configurations
{
    /// <summary>
    /// EF Core mapping for <see cref="InvoiceHeader"/>. Keeps all persistence concerns
    /// out of the domain type (which stays free of data annotations).
    /// </summary>
    public class InvoiceHeaderConfiguration : IEntityTypeConfiguration<InvoiceHeader>
    {
        public void Configure(EntityTypeBuilder<InvoiceHeader> builder)
        {
            builder.ToTable("InvoiceHeader");

            builder.HasKey(e => e.InvoiceId);
            builder.Property(e => e.InvoiceId).ValueGeneratedOnAdd();

            builder.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            builder.HasIndex(e => e.InvoiceNumber).IsUnique();
            builder.Property(e => e.Address);
            builder.Property(e => e.InvoiceDate);
            builder.Property(e => e.InvoiceTotal);

            builder.HasMany(e => e.Lines)
                .WithOne(l => l.Invoice!)
                .HasForeignKey(l => l.InvoiceId)
                .IsRequired();

            // Read-only navigation is backed by the private _lines field.
            builder.Metadata
                .FindNavigation(nameof(InvoiceHeader.Lines))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
