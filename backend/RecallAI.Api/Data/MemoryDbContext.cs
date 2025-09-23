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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will only be used if DbContext is created without DI
            // The main configuration is in Program.cs
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Preferences).HasColumnName("preferences").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        });

        // Memory configuration
        modelBuilder.Entity<Memory>(entity =>
        {
            entity.ToTable("memories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("content_type").HasDefaultValue("text");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Profile)
                  .WithMany(p => p.Memories)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Configure many-to-many relationship with Collections
            entity.HasMany(e => e.Collections)
                  .WithMany(c => c.Memories)
                  .UsingEntity<MemoryCollection>(
                      j => j.HasOne(mc => mc.Collection)
                            .WithMany()
                            .HasForeignKey(mc => mc.CollectionId),
                      j => j.HasOne(mc => mc.Memory)
                            .WithMany()
                            .HasForeignKey(mc => mc.MemoryId),
                      j =>
                      {
                          j.ToTable("memory_collections");
                          j.HasKey(mc => new { mc.MemoryId, mc.CollectionId });
                          j.Property(mc => mc.MemoryId).HasColumnName("memory_id");
                          j.Property(mc => mc.CollectionId).HasColumnName("collection_id");
                          j.Property(mc => mc.AddedAt).HasColumnName("added_at").HasDefaultValueSql("now()");
                      });
        });

        // MemoryEmbedding configuration
        modelBuilder.Entity<MemoryEmbedding>(entity =>
        {
            entity.ToTable("memory_embeddings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MemoryId).HasColumnName("memory_id");
            entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1536)");
            entity.Property(e => e.ModelName).HasColumnName("model_name").HasDefaultValue("text-embedding-3-small");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Memory)
                  .WithMany(m => m.MemoryEmbeddings)
                  .HasForeignKey(e => e.MemoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("collections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Color).HasColumnName("color").HasDefaultValue("#6366f1");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Profile)
                  .WithMany(p => p.Collections)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}