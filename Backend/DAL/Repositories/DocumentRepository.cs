using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using System;

namespace Swen3.API.DAL.Repositories
{
    public class DocumentRepository : EfRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(AppDbContext ctx) : base(ctx) { }
        public async Task<Document?> GetWithTagsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _ctx.Documents
            .Include(d => d.DocumentTags).ThenInclude(dt => dt.Tag)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }
    }
}
