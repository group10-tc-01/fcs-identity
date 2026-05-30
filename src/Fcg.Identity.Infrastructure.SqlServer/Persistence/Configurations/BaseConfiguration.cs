using Fcg.Identity.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Identity.Infrastructure.SqlServer.Persistence.Configurations;

public abstract class BaseConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(entity => entity.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
