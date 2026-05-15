using MediaService.Domain.Files;
using MediaService.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaService.Infrastructure.Configurations;

public sealed class NodeEntityConfiguration : IEntityTypeConfiguration<NodeEntity>
{
    public void Configure(EntityTypeBuilder<NodeEntity> entity)
    {
        entity
            .ToTable("nodes")
            .HasKey(node => node.Id);

        entity.Property(node => node.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        entity.Property(node => node.ParentId)
            .HasColumnName("parent_id");

        entity.Property(node => node.Name)
            .HasColumnName("name")
            .IsRequired();

        entity.Property(node => node.Type)
            .HasColumnName("type")
            .HasColumnType("node_type")
            .HasDefaultValue(NodeType.Folder)
            .IsRequired();

        entity.Property(node => node.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        entity.Property(node => node.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(node => node.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        entity.HasOne(node => node.Parent)
                    .WithMany(node => node.Children)
                    .HasForeignKey(node => node.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(node => node.File)
            .WithOne(file => file.Node)
            .HasForeignKey<FileEntity>(file => file.NodeId);
    }
}
