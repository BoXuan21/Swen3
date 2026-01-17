using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL.Models;

namespace Swen3.API.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Priority> Priorities => Set<Priority>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed priority data
            modelBuilder.Entity<Priority>().HasData(
                new Priority { Id = 1, Name = "Not Very Important", Level = 1 },
                new Priority { Id = 2, Name = "Important", Level = 2 },
                new Priority { Id = 3, Name = "Very Important", Level = 3 }
            );
        }
    }
}
