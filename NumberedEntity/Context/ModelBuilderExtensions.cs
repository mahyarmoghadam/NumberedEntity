using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NumberedEntity.Context
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyNumberedEntityConfiguration(this ModelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.ApplyConfiguration(new NumberedEntityConfiguration());
        }
    }

    public class NumberedEntityConfiguration : IEntityTypeConfiguration<Models.NumberedEntity>
    {
        public void Configure(EntityTypeBuilder<Models.NumberedEntity> builder)
        {
            builder.Property(a => a.EntityName).HasMaxLength(256).IsRequired().IsUnicode(false);

            builder.HasIndex(a => a.EntityName).HasDatabaseName("UIX_NumberedEntity_EntityName").IsUnique();

            builder.ToTable(nameof(NumberedEntity));
        }
    }
}