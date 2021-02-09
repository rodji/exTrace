using System.Collections.Concurrent;

namespace exTrace
{
    public static class CBagExt
    {
        public static void Clear<T>(this ConcurrentBag<T> bag)
            where T: class
        {
            while (!bag.IsEmpty)
            {
                bag.TryTake(out T _);
            }
        }
    }
}