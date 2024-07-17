using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace TestTaskMessanger.Utils
{
    public class MesMemoryCache : IMemoryCache
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<object, object> _keys;

        public MesMemoryCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
            _keys = new ConcurrentDictionary<object, object>();
        }

        public ICacheEntry CreateEntry(object key)
        {
            _keys[key] = null!;
            return _cache.CreateEntry(key);
        }

        public void Remove(object key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        public bool TryGetValue(object key, out object value) => _cache.TryGetValue(key, out value);
        public IEnumerable<object> GetKeys() => _keys.Keys;
        public void Dispose() => _cache.Dispose();

    }
}
