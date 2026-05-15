using MediaService.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaService.Infrastructure.Configurations;

public sealed class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> entity)
    {
        entity.ToTable("files", table =>
        {
            table.HasCheckConstraint(
                "chk_files_size",
                "size > 0");

            table.HasCheckConstraint(
                "chk_files_status_storage_url",
                """
                (
                    status = 'completed'
                    and storage_url is not null
                )
                or
                (
                    status in ('pending', 'failed')
                    and storage_url is null
                )
                """);
        })
        .HasKey(file => file.NodeId);

        entity.Property(file => file.NodeId)
            .HasColumnName("node_id");

        entity.Property(file => file.MimeType)
            .HasColumnName("mime_type")
            .IsRequired();

        entity.Property(file => file.Size)
            .HasColumnName("size")
            .IsRequired();

        entity.Property(file => file.StorageUrl)
            .HasColumnName("storage_url");

        entity.HasIndex(file => file.StorageUrl)
            .IsUnique();

        entity.Property(file => file.Status)
            .HasColumnName("status")
            .HasColumnType("upload_status")
            .HasDefaultValue(UploadStatus.Pending)
            .IsRequired();

        entity.Property(file => file.UploadExpiresAt)
            .HasColumnName("upload_expires_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() + interval '15 minutes'")
            .IsRequired();

        entity.Property(file => file.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        entity.Property(file => file.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        entity.HasOne(file => file.Node)
            .WithOne(node => node.File)
            .HasForeignKey<FileEntity>(file => file.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
