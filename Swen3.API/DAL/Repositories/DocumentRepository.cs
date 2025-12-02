using Microsoft.EntityFrameworkCore;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _ctx;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(AppDbContext ctx, ILogger<DocumentRepository> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        public async Task<IEnumerable<Document>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all documents");
                var documents = await _ctx.Documents
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} documents", documents.Count);
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                throw new RepositoryException("Failed to retrieve documents", ex);
            }
        }

        public async Task<Document?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("Retrieving document with id: {DocumentId}", id);
                var document = await _ctx.Documents
                    .FirstOrDefaultAsync(d => d.Id == id);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with id: {DocumentId}", id);
                throw new RepositoryException($"Failed to retrieve document with id {id}", ex);
            }
        }

        public async Task AddAsync(Document doc)
        {
            try
            {
                _logger.LogInformation("Adding document with id: {DocumentId}, title: {Title}", doc.Id, doc.Title);
                await _ctx.Documents.AddAsync(doc);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully added document with id: {DocumentId}", doc.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding document with id: {DocumentId}", doc.Id);
                throw new RepositoryException("Failed to save document to database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding document with id: {DocumentId}", doc.Id);
                throw new RepositoryException("Failed to add document", ex);
            }
        }

        public async Task UpdateAsync(Document doc)
        {
            try
            {
                _logger.LogInformation("Updating document with id: {DocumentId}", doc.Id);
                _ctx.Documents.Update(doc);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully updated document with id: {DocumentId}", doc.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating document with id: {DocumentId}", doc.Id);
                throw new RepositoryException("Document was modified by another operation", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating document with id: {DocumentId}", doc.Id);
                throw new RepositoryException("Failed to update document in database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating document with id: {DocumentId}", doc.Id);
                throw new RepositoryException("Failed to update document", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting document with id: {DocumentId}", id);
                var doc = await _ctx.Documents.FindAsync(id);
                if (doc == null)
                {
                    _logger.LogWarning("Document with id: {DocumentId} not found for deletion", id);
                    throw new NotFoundException("Document", id);
                }

                _ctx.Documents.Remove(doc);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted document with id: {DocumentId}", id);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting document with id: {DocumentId}", id);
                throw new RepositoryException("Failed to delete document from database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting document with id: {DocumentId}", id);
                throw new RepositoryException("Failed to delete document", ex);
            }
        }
    }
}
