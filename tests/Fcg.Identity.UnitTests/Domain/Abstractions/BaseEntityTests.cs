using Fcg.Identity.Domain.Abstractions;
using FluentAssertions;

namespace Fcg.Identity.UnitTests.Domain.Abstractions;

public sealed class BaseEntityTests
{
    [Fact]
    public void Given_EntityCreated_When_IdIsProvided_Then_ShouldSetIdAndCreatedAt()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id);

        // Assert
        entity.Id.Should().Be(id);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Given_Deactivate_Called_When_EntityIsActive_Then_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        entity.Deactivate();

        // Assert
        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Given_Activate_Called_When_EntityIsInactive_Then_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        entity.Deactivate();

        // Act
        entity.Activate();

        // Assert
        entity.IsActive.Should().BeTrue();
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Given_DomainEventRaised_When_GetDomainEventsIsCalled_Then_ShouldReturnRaisedDomainEvents()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        // Act
        entity.Raise(domainEvent);

        // Assert
        entity.GetDomainEvents().Should().ContainSingle().Which.Should().Be(domainEvent);
    }

    [Fact]
    public void Given_ClearDomainEvents_Called_When_EntityHasDomainEvents_Then_ShouldRemoveDomainEvents()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        entity.Raise(new TestDomainEvent());

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.GetDomainEvents().Should().BeEmpty();
    }

    private sealed class TestEntity : BaseEntity
    {
        public TestEntity(Guid id)
            : base(id)
        {
        }

        public void Raise(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }

    private sealed record TestDomainEvent : IDomainEvent;
}
