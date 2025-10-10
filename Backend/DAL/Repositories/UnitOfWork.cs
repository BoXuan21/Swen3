using Swen3.API.DAL;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using System;

namespace Swen3.API.DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _ctx;
        public UnitOfWork(AppDbContext ctx)
        {
            _ctx = ctx;
            Documents = new DocumentRepository(_ctx);
            Tags = new EfRepository<Tag>(_ctx);
            Users = new EfRepository<User>(_ctx);
        }

        public IDocumentRepository Documents { get; }
        public IRepository<Tag> Tags { get; }
        public IRepository<User> Users { get; }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _ctx.SaveChangesAsync(cancellationToken);

        public void Dispose() => _ctx.Dispose();
    }
}
