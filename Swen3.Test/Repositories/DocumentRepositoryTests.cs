using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL;
using Swen3.API.DAL.Repositories;
using Swen3.API.DAL.Models;

namespace Swen3.Test.Repositories;

[TestFixture]
public class DocumentRepositoryTests
{
    private AppDbContext _context = null!;
    private DocumentRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var logger = NullLogger<DocumentRepository>.Instance;
        _repository = new DocumentRepository(_context, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenDocumentExists_ShouldReturnDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            Title = "Test Document",
            FileName = "test.pdf",
            MimeType = "application/pdf",
            Size = 1024,
            StorageKey = ""
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(documentId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(documentId, result!.Id);
        Assert.AreEqual("Test Document", result.Title);
    }

    [Test]
    public async Task GetByIdAsync_WhenDocumentDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task AddAsync_ShouldAddDocumentToDatabase()
    {
        // Arrange
        var document = new Document
        {
            Title = "New Document",
            FileName = "new.pdf",
            MimeType = "application/pdf",
            Size = 512,
            StorageKey = ""
        };

        // Act
        await _repository.AddAsync(document);

        // Assert
        var result = await _context.Documents.FirstOrDefaultAsync(d => d.Title == "New Document");
        Assert.IsNotNull(result);
        Assert.AreEqual("new.pdf", result!.FileName);
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveDocumentFromDatabase()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "To Delete",
            FileName = "delete.pdf",
            MimeType = "application/pdf",
            Size = 512,
            StorageKey = ""
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(document.Id);

        // Assert
        var exists = await _context.Documents.AnyAsync(d => d.Id == document.Id);
        Assert.IsFalse(exists);
    }

    [Test]
    public async Task DeleteAsync_WhenDocumentDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _repository.DeleteAsync(nonExistentId));
        Assert.IsNotNull(ex);
        Assert.That(ex.Message, Does.Contain("Document"));
        Assert.That(ex.Message, Does.Contain(nonExistentId.ToString()));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateDocument()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            FileName = "file.pdf",
            MimeType = "application/pdf",
            Size = 100,
            StorageKey = ""
        };
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        // Act
        document.Title = "Updated Title";
        await _repository.UpdateAsync(document);

        // Assert
        var updated = await _context.Documents.FindAsync(document.Id);
        Assert.AreEqual("Updated Title", updated!.Title);
    }
}
