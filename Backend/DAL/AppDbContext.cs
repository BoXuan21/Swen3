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

            // Seed a system user for documents without specific uploaders
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Username = "System",
                    Email = "system@swen3.local"
                }
            );

            modelBuilder.Entity<DocumentTag>().HasKey(dt => new { dt.DocumentId, dt.TagId });
            modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();
        }
    }
}
