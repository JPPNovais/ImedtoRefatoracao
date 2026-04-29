using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Idempotency;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(k => k.Key);
        builder.Property(k => k.Key).HasColumnName("key").HasMaxLength(80).IsRequired();

        builder.Property(k => k.HashPayload).HasColumnName("hash_payload").HasMaxLength(64).IsRequired();
        builder.Property(k => k.StatusCode).HasColumnName("status_code").IsRequired();
        builder.Property(k => k.ResponseJson).HasColumnName("response_json").IsRequired();
        builder.Property(k => k.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(k => k.ExpiraEm).HasColumnName("expira_em").IsRequired();
    }
}
