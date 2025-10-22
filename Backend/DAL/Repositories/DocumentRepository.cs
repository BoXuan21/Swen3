using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using System;

namespace Swen3.API.DAL.Repositories
{
    public class DocumentRepository
    {
        private readonly AppDbContext _ctx;

        public DocumentRepository(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IEnumerable<Document>> GetAllAsync()
        {
            return await _ctx.Documents
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<Document?> GetByIdAsync(Guid id)
        {
            return await _ctx.Documents
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task AddAsync(Document doc)
        {
            await _ctx.Documents.AddAsync(doc);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Document doc)
        {
            _ctx.Documents.Update(doc);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var doc = await _ctx.Documents.FindAsync(id);
            if (doc == null) return;

            _ctx.Documents.Remove(doc);
            await _ctx.SaveChangesAsync();
        }
    }
}
