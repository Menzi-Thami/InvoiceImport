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
        public void Configure(EntityTypeBuilder<InvoiceHeader> entity)
        {
            entity.ToTable("InvoiceHeader");

            entity.HasKey(e => e.InvoiceId);
            entity.Property(e => e.InvoiceId).ValueGeneratedOnAdd();

            entity.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.Address);
            entity.Property(e => e.InvoiceDate);
            entity.Property(e => e.InvoiceTotal);

            entity.HasMany(e => e.Lines)
                .WithOne(l => l.Invoice!)
                .HasForeignKey(l => l.InvoiceId)
                .IsRequired();

            // Read-only navigation is backed by the private _lines field.
            entity.Metadata
                .FindNavigation(nameof(InvoiceHeader.Lines))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
