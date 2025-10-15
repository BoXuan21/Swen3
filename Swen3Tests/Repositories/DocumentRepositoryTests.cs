using Microsoft.EntityFrameworkCore;
using Moq;
using Swen3.API.DAL;
using Swen3.API.DAL.Models;
using Swen3.API.DAL.Repositories;

namespace Swen3Tests.Repositories;

[TestFixture]
public class DocumentRepositoryTests
{
    private Mock<AppDbContext> _mockContext;
    private DocumentRepository _repository;

    [SetUp]
    public void Setup()
    {
        // Setup mock database context
        _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _repository = new DocumentRepository(_mockContext.Object);
    }

    [Test]
    public async Task GetByIdAsync_WhenDocumentExists_ShouldReturnDocument()
    {
        // Arrange - Create a test document
        var documentId = Guid.NewGuid();
        var document = new Document 
        { 
            Id = documentId, 
            Title = "Test Document",
            FileName = "test.pdf",
            MimeType = "application/pdf",
            Size = 1024
        };

        // Mock the database to return our test document
        _mockContext.Setup(c => c.Set<Document>().FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(document);

        // Act - Try to get the document
        var result = await _repository.GetByIdAsync(documentId);

        // Assert - Verify we got the right document back
        Assert.IsNotNull(result);
        Assert.AreEqual(documentId, result!.Id);
        Assert.AreEqual("Test Document", result.Title);
    }

    [Test]
    public async Task GetByIdAsync_WhenDocumentDoesNotExist_ShouldReturnNull()
    {
        // Arrange - Setup mock to return null (document not found)
        _mockContext.Setup(c => c.Set<Document>().FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Document?)null);

        // Act - Try to get a non-existent document
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert - Should return null
        Assert.IsNull(result);
    }

    [Test]
    public async Task AddAsync_ShouldCallDatabaseAdd()
    {
        // Arrange - Create a document to add
        var document = new Document 
        { 
            Title = "New Document",
            FileName = "new.pdf",
            MimeType = "application/pdf",
            Size = 512
        };

        // Setup mock DbSet
        var mockDbSet = new Mock<DbSet<Document>>();
        _mockContext.Setup(c => c.Set<Document>()).Returns(mockDbSet.Object);

        // Act - Add the document
        await _repository.AddAsync(document);

        // Assert - Verify AddAsync was called on the database
        mockDbSet.Verify(ds => ds.AddAsync(document, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Update_ShouldCallDatabaseUpdate()
    {
        // Arrange - Create a document to update
        var document = new Document 
        { 
            Id = Guid.NewGuid(),
            Title = "Updated Document",
            FileName = "updated.pdf",
            MimeType = "application/pdf",
            Size = 1024
        };

        // Setup mock DbSet
        var mockDbSet = new Mock<DbSet<Document>>();
        _mockContext.Setup(c => c.Set<Document>()).Returns(mockDbSet.Object);

        // Act - Update the document
        _repository.Update(document);

        // Assert - Verify Update was called on the database
        mockDbSet.Verify(ds => ds.Update(document), Times.Once);
    }

    [Test]
    public void Remove_ShouldCallDatabaseRemove()
    {
        // Arrange - Create a document to remove
        var document = new Document 
        { 
            Id = Guid.NewGuid(),
            Title = "Document to Remove",
            FileName = "remove.pdf",
            MimeType = "application/pdf",
            Size = 1024
        };

        // Setup mock DbSet
        var mockDbSet = new Mock<DbSet<Document>>();
        _mockContext.Setup(c => c.Set<Document>()).Returns(mockDbSet.Object);

        // Act - Remove the document
        _repository.Remove(document);

        // Assert - Verify Remove was called on the database
        mockDbSet.Verify(ds => ds.Remove(document), Times.Once);
    }
}