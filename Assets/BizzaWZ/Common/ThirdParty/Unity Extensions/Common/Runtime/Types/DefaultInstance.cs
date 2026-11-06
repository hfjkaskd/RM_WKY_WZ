
namespace UnityExtensions
{
    public static class DefaultInstance<T> where T : new()
    {
        public static readonly T value = new T();
    }
}
