using Microsoft.Extensions.Caching.Memory;

namespace TestTaskMessanger.Utils
{
    public static class MemoryCacheExtensions
    {
        public static IEnumerable<string> GetKeys<T>(this IMemoryCache memoryCache)
        {
            var cacheEntries = memoryCache as IEnumerable<KeyValuePair<object, object>>;
            if (cacheEntries != null)
            {
                foreach (var cacheEntry in cacheEntries)
                {
                    if (cacheEntry.Key is string key && cacheEntry.Value is T)
                    {
                        yield return key;
                    }
                }
            }
        }
    }
}
