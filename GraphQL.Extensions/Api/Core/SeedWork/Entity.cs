namespace Api.Core.SeedWork;

public interface IEntity<TId>
{
    TId Id { get; }
}

public abstract class Entity<TId> : IEntity<TId>
{
    public TId Id { get; }

    protected Entity()
    {
        Id = default!;
    }

    protected Entity(TId id)
    {
        Id = id;
    }
}

public abstract class Entity : Entity<Guid>
{
    protected Entity() : base(Guid.NewGuid()) { }

    protected Entity(Guid id) : base(id) { }
}