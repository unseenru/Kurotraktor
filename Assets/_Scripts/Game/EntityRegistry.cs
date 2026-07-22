public class EntityRegistry<T> : IEntityRegistry<T>
{
    public T Current { get; private set; }
    public bool HasTarget => Current != null;

    public void Register(T entity) => Current = entity;
    public void Unregister(T entity)
    {
        if (Equals(Current, entity))
            Current = default;
    }
}