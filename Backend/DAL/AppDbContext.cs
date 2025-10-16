using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Swen3.API.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
        public DbSet<User> Users => Set<User>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Document-User relationship as optional
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedById)
                .IsRequired(false); // This makes the foreign key optional

            modelBuilder.Entity<DocumentTag>().HasKey(dt => new { dt.DocumentId, dt.TagId });
            modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();
        }
    }
}
