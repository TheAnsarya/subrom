namespace Subrom.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots.
/// Aggregate roots are the entry point for accessing a cluster of related entities.
/// </summary>
public interface IAggregateRoot {
	IReadOnlyList<IDomainEvent> DomainEvents { get; }
	void ClearDomainEvents();
}

/// <summary>
/// Base class for aggregate roots with domain event support.
/// </summary>
public abstract class AggregateRoot : Entity, IAggregateRoot {
	private readonly List<IDomainEvent> _domainEvents = [];

	public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

	protected AggregateRoot() : base() { }

	protected AggregateRoot(Guid id) : base(id) { }

	protected void AddDomainEvent(IDomainEvent domainEvent) {
		_domainEvents.Add(domainEvent);
	}

	public void ClearDomainEvents() {
		_domainEvents.Clear();
	}
}
