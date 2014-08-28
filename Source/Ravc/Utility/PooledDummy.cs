namespace Ravc.Utility
{
    public class PooledDummy<T> : IPooled<T>
    {
        public T Item { get; private set; }

        public PooledDummy(T item)
        {
            Item = item;
        }

        public void IncRefCount() { }
        public void Release() { }
    }
}