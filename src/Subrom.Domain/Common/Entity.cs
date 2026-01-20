namespace Subrom.Domain.Common;

/// <summary>
/// Base class for domain entities with unique identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull {
	public TId Id { get; protected init; } = default!;

	public override bool Equals(object? obj) =>
		obj is Entity<TId> other && Equals(other);

	public bool Equals(Entity<TId>? other) =>
		other is not null && EqualityComparer<TId>.Default.Equals(Id, other.Id);

	public override int GetHashCode() =>
		EqualityComparer<TId>.Default.GetHashCode(Id);

	public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
		left?.Equals(right) ?? right is null;

	public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
		!(left == right);
}

/// <summary>
/// Entity with GUID identifier.
/// </summary>
public abstract class Entity : Entity<Guid> {
	protected Entity() {
		Id = Guid.NewGuid();
	}

	protected Entity(Guid id) {
		Id = id;
	}
}
