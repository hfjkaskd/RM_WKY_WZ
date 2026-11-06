namespace Bizza
{
    public interface IPool<T> where T : class
    {
        T Get();
        void Release(T item);

        void Release(object item)
        {
            if (item is T t)
            {
                Release(t);
            }
        }
        PoolItemOwner<T> GetAutoRelease() => new(Get(), this);
    }
}