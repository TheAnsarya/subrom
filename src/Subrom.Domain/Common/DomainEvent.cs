namespace Subrom.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// </summary>
public interface IDomainEvent {
	DateTime OccurredAt { get; }
}

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent {
	public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
