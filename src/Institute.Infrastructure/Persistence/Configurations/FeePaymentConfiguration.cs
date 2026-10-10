using Institute.Domain.Batches;
using Institute.Domain.Payments;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Institute.Infrastructure.Persistence.Configurations;

internal sealed class FeePaymentConfiguration : IEntityTypeConfiguration<FeePayment>
{
    public void Configure(EntityTypeBuilder<FeePayment> builder)
    {
        builder.ToTable("FeePayments", t => t.HasCheckConstraint("CK_FeePayments_Amount", "[Amount] > 0"));
        builder.ConfigureAggregateRoot<FeePayment, FeePaymentId>();

        builder.ComplexProperty(p => p.Amount, amount =>
        {
            amount.Property(m => m.Amount).HasColumnName("Amount");
            amount.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength().IsRequired();
        });

        builder.Property(p => p.ReceiptNumber).HasMaxLength(FeePayment.ReceiptNumberMaxLength).IsRequired();
        builder.Property(p => p.TransactionReference).HasMaxLength(FeePayment.ReferenceMaxLength);
        builder.Property(p => p.RefundReason).HasMaxLength(FeePayment.RefundReasonMaxLength);
        builder.Property(p => p.Method).IsRequired();
        builder.Property(p => p.Status).IsRequired();

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Batch>()
            .WithMany()
            .HasForeignKey(p => p.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ReceiptNumber).IsUnique();
        builder.HasIndex(p => new { p.StudentId, p.BatchId });
    }
}
