using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneIncTask.Domain.Entities;

namespace OneIncTask.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(j => j.InputText)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(j => j.ExpectedResult)
            .HasMaxLength(50000);

        builder.Property(j => j.CurrentResult)
            .HasMaxLength(50000);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(j => new { j.UserId, j.Status });
        builder.HasIndex(j => j.CreatedAt);
    }
}
