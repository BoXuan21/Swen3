using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Document?> GetWithTagsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
