using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL;
using Swen3.API.DAL.Interfaces;
using System;

namespace Swen3.API.DAL.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _ctx;
        public EfRepository(AppDbContext ctx) => _ctx = ctx;


        public async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await _ctx.Set<T>().AddAsync(entity, cancellationToken);
        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => await _ctx.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        public async Task<IEnumerable<T>> ListAsync(CancellationToken cancellationToken = default) => await _ctx.Set<T>().ToListAsync(cancellationToken);
        public void Remove(T entity) => _ctx.Set<T>().Remove(entity);
        public void Update(T entity) => _ctx.Set<T>().Update(entity);
    }
}
