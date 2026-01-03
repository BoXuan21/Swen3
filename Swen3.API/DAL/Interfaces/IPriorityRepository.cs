using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Interfaces
{
    public interface IPriorityRepository
    {
        Task<IEnumerable<Priority>> GetAllAsync();
        Task<Priority?> GetByIdAsync(int id);
        Task AddAsync(Priority priority);
        Task UpdateAsync(Priority priority);
        Task DeleteAsync(int id);
    }
}

