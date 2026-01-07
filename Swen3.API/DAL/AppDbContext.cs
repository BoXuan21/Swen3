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
        public DbSet<Priority> Priorities => Set<Priority>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // when the database is created, the priority data will be seeded, user does not need to create these
            modelBuilder.Entity<Priority>().HasData(
                new Priority { Id = 1, Name = "Not Very Important", Level = 1 },
                new Priority { Id = 2, Name = "Important", Level = 2 },
                new Priority { Id = 3, Name = "Very Important", Level = 3 }
            );
        }
    }
}
