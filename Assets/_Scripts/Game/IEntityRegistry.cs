public interface IEntityRegistry<T>
{
    T Current { get; }
    bool HasTarget { get; }

    void Register(T entity);
    void Unregister(T entity);
}
