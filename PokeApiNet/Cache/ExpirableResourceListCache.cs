using Microsoft.Extensions.Caching.Memory;
using PokeApiNet.Models;
using System;

namespace PokeApiNet.Cache
{
    internal sealed class ExpirableResourceListCache : BaseExpirableCache
    {
        private readonly MemoryCache urlCache;

        public ExpirableResourceListCache(CacheOptions cacheOptions, IObservable<CacheExpirationOptions> expirationOptionsProvider)
            : base(expirationOptionsProvider)
        {
            urlCache = new MemoryCache(cacheOptions.ToMemoryCacheOptions());
        }

        public void Store<T>(string url, ResourceList<T> resourceList)
            where T : ResourceBase
        {
            urlCache.Set(url, resourceList, CacheEntryOptions);
        }

        public ResourceList<T> Get<T>(string url) where T : ResourceBase => urlCache.Get<ResourceList<T>>(url);

        public override void Dispose()
        {
            // Ensures that created cache entries are expired
            base.Dispose();
            urlCache.Dispose();
        }
    }
}
