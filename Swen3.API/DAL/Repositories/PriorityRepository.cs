using Microsoft.EntityFrameworkCore;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Repositories
{
    public class PriorityRepository : IPriorityRepository
    {
        private readonly AppDbContext _ctx;
        private readonly ILogger<PriorityRepository> _logger;

        public PriorityRepository(AppDbContext ctx, ILogger<PriorityRepository> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        public async Task<IEnumerable<Priority>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all priorities");
                var priorities = await _ctx.Priorities
                    .OrderByDescending(p => p.Level)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} priorities", priorities.Count);
                return priorities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all priorities");
                throw new RepositoryException("Failed to retrieve priorities", ex);
            }
        }

        public async Task<Priority?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving priority with id: {PriorityId}", id);
                var priority = await _ctx.Priorities
                    .FirstOrDefaultAsync(p => p.Id == id);
                return priority;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving priority with id: {PriorityId}", id);
                throw new RepositoryException($"Failed to retrieve priority with id {id}", ex);
            }
        }

        public async Task AddAsync(Priority priority)
        {
            try
            {
                _logger.LogInformation("Adding priority with name: {Name}", priority.Name);
                await _ctx.Priorities.AddAsync(priority);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully added priority with id: {PriorityId}", priority.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding priority with name: {Name}", priority.Name);
                throw new RepositoryException("Failed to save priority to database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding priority with name: {Name}", priority.Name);
                throw new RepositoryException("Failed to add priority", ex);
            }
        }

        public async Task UpdateAsync(Priority priority)
        {
            try
            {
                _logger.LogInformation("Updating priority with id: {PriorityId}", priority.Id);
                _ctx.Priorities.Update(priority);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully updated priority with id: {PriorityId}", priority.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating priority with id: {PriorityId}", priority.Id);
                throw new RepositoryException("Priority was modified by another operation", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating priority with id: {PriorityId}", priority.Id);
                throw new RepositoryException("Failed to update priority in database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating priority with id: {PriorityId}", priority.Id);
                throw new RepositoryException("Failed to update priority", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting priority with id: {PriorityId}", id);
                var priority = await _ctx.Priorities.FindAsync(id);
                if (priority == null)
                {
                    _logger.LogWarning("Priority with id: {PriorityId} not found for deletion", id);
                    throw new NotFoundException("Priority", id);
                }

                _ctx.Priorities.Remove(priority);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted priority with id: {PriorityId}", id);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting priority with id: {PriorityId}", id);
                throw new RepositoryException("Failed to delete priority from database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting priority with id: {PriorityId}", id);
                throw new RepositoryException("Failed to delete priority", ex);
            }
        }
    }
}

