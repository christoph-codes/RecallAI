using Microsoft.EntityFrameworkCore;
using RecallAI.Api.Models;
using Pgvector.EntityFrameworkCore;

namespace RecallAI.Api.Data;

public class MemoryDbContext : DbContext
{
    public MemoryDbContext(DbContextOptions<MemoryDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Memory> Memories { get; set; }
    public DbSet<MemoryEmbedding> MemoryEmbeddings { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<MemoryCollection> MemoryCollections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Memory configuration
        modelBuilder.Entity<Memory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Metadata).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Profile)
                  .WithMany(p => p.Memories)
                  .HasForeignKey(e => e.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MemoryEmbedding configuration
        modelBuilder.Entity<MemoryEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Embedding).HasColumnType("vector(1536)"); // OpenAI text-embedding-3-small dimension
            entity.Property(e => e.Model).HasMaxLength(50).HasDefaultValue("text-embedding-3-small");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Memory)
                  .WithOne(m => m.Embedding)
                  .HasForeignKey<MemoryEmbedding>(e => e.MemoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Color).HasMaxLength(50).HasDefaultValue("#3B82F6");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Profile)
                  .WithMany(p => p.Collections)
                  .HasForeignKey(e => e.ProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MemoryCollection configuration (many-to-many)
        modelBuilder.Entity<MemoryCollection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Memory)
                  .WithMany(m => m.MemoryCollections)
                  .HasForeignKey(e => e.MemoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.MemoryCollections)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique memory-collection pairs
            entity.HasIndex(e => new { e.MemoryId, e.CollectionId }).IsUnique();
        });
    }
}