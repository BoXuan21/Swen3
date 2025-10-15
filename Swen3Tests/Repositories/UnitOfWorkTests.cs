using Microsoft.EntityFrameworkCore;
using Moq;
using Swen3.API.DAL;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using Swen3.API.DAL.Repositories;

namespace Swen3Tests.Repositories;

[TestFixture]
public class UnitOfWorkTests
{
    private Mock<AppDbContext> _mockContext;
    private UnitOfWork _unitOfWork;

    [SetUp]
    public void Setup()
    {
        // Setup mock database context
        _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _unitOfWork = new UnitOfWork(_mockContext.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up resources
        _unitOfWork?.Dispose();
    }

    [Test]
    public void Constructor_ShouldInitializeAllRepositories()
    {
        // Act & Assert - Verify all repositories are created
        Assert.IsNotNull(_unitOfWork.Documents);
        Assert.IsInstanceOf<DocumentRepository>(_unitOfWork.Documents);
        
        Assert.IsNotNull(_unitOfWork.Tags);
        Assert.IsInstanceOf<IRepository<Tag>>(_unitOfWork.Tags);
        
        Assert.IsNotNull(_unitOfWork.Users);
        Assert.IsInstanceOf<IRepository<User>>(_unitOfWork.Users);
    }

    [Test]
    public async Task SaveChangesAsync_ShouldCallContextSaveChanges()
    {
        // Arrange - Setup mock to return number of changes saved
        var expectedChanges = 3;
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedChanges);

        // Act - Save changes
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert - Verify the correct number of changes were saved
        Assert.AreEqual(expectedChanges, result);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Dispose_ShouldDisposeContext()
    {
        // Act - Dispose the unit of work
        _unitOfWork.Dispose();

        // Assert - Verify context was disposed
        _mockContext.Verify(c => c.Dispose(), Times.Once);
    }
}