using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dataset> Datasets => Set<Dataset>();

    public DbSet<DatasetField> DatasetFields => Set<DatasetField>();

    public DbSet<SavedReport> SavedReports => Set<SavedReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dataset>(entity =>
        {
            entity.ToTable("Datasets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ViewName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<DatasetField>(entity =>
        {
            entity.ToTable("Fields");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FieldName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DataType).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.DatasetId);
            entity.HasIndex(x => new { x.DatasetId, x.FieldName }).IsUnique();

            entity.HasOne(x => x.Dataset)
                .WithMany(x => x.Fields)
                .HasForeignKey(x => x.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedReport>(entity =>
        {
            entity.ToTable("SavedReports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DefinitionJson).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
