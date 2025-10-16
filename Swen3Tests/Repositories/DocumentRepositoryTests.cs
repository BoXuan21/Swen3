using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL;
using Swen3.API.DAL.Repositories;
using Swen3.API.DAL.Models;
using Swen3.API.DAL.Interfaces;

namespace Swen3Tests.Repositories;

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
        _repository = new DocumentRepository(_context);
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
            Size = 1024
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
            Size = 512
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
            Size = 512
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
    public async Task UpdateAsync_ShouldUpdateDocument()
    {
        // Arrange
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            FileName = "file.pdf",
            MimeType = "application/pdf",
            Size = 100
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
ç