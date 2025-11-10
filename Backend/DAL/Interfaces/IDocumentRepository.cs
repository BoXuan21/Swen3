using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Interfaces
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<Document>> GetAllAsync();
        Task<Document?> GetByIdAsync(Guid id);
        Task AddAsync(Document doc);
        Task UpdateAsync(Document doc);
        Task DeleteAsync(Guid id);
    }
}
