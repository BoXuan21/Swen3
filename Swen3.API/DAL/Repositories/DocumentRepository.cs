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
                    .Include(d => d.Priority)
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
                    .Include(d => d.Priority)
                    .FirstOrDefaultAsync(d => d.Id == id);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with id: {DocumentId}", id);
                throw new RepositoryException($"Failed to retrieve document with id {id}", ex);
            }
        }

        public async Task<IEnumerable<Document>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            try
            {
                _logger.LogDebug("Retrieving documents by {Count} ids", ids.Count());
                var idList = ids.ToList();
                var documents = await _ctx.Documents
                    .Include(d => d.Priority)
                    .Where(d => idList.Contains(d.Id))
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by ids");
                throw new RepositoryException("Failed to retrieve documents by ids", ex);
            }
        }

        public async Task<IEnumerable<Document>> SearchAsync(IEnumerable<Guid>? documentIds, int? priorityId)
        {
            try
            {
                _logger.LogInformation("Searching documents with priorityId: {PriorityId}, documentIds count: {Count}",
                    priorityId, documentIds?.Count() ?? 0);

                //starts with all documents
                var query = _ctx.Documents
                    .Include(d => d.Priority)
                    .AsQueryable();

                // If we have document IDs from Elasticsearch, filter by them
                if (documentIds != null)
                {
                    var idList = documentIds.ToList();
                    query = query.Where(d => idList.Contains(d.Id));
                }

                // Filter by priority if specified
                if (priorityId.HasValue)
                {
                    query = query.Where(d => d.PriorityId == priorityId.Value);
                }
                // Execute and return
                var documents = await query
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                _logger.LogInformation("Search returned {Count} documents", documents.Count);
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents");
                throw new RepositoryException("Failed to search documents", ex);
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

        public async Task UpdateSummaryAsync(Guid id, string summary)
        {
            try
            {
                var doc = await _ctx.Documents.FirstOrDefaultAsync(d => d.Id == id);
                if (doc == null)
                {
                    throw new RepositoryException("Document not found");
                }
                doc.Summary = summary;
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Updating document with id: {DocumentId}", doc.Id);
                _ctx.Documents.Update(doc);
                await _ctx.SaveChangesAsync();
                _logger.LogInformation("Successfully updated document with id: {DocumentId}", doc.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating document with id: {DocumentId}", id);
                throw new RepositoryException("Document was modified by another operation", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating document with id: {DocumentId}", id);
                throw new RepositoryException("Failed to update document in database", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating document with id: {DocumentId}", id);
                throw new RepositoryException("Failed to update document", ex);
            }
        }
    }
}
