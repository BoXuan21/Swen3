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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
