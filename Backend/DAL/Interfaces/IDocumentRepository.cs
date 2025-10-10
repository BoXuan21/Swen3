using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Interfaces
{
    public interface IDocumentRepository : IRepository<Document>
    {
        Task<Document?> GetWithTagsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
