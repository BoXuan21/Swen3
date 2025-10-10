using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDocumentRepository Documents { get; }
        IRepository<Tag> Tags { get; }
        IRepository<User> Users { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
