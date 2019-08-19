using Microsoft.Extensions.Caching.Memory;
using PokeApiNet.Models;
using System;

namespace PokeApiNet.Cache
{
    internal sealed class ExpirableResourceCache : BaseExpirableCache
    {
        private readonly MemoryCache IdCache;
        private readonly MemoryCache NameCache;

        public ExpirableResourceCache(CacheOptions cacheOptions, IObservable<CacheExpirationOptions> expirationOptionsProvider)
            : base(expirationOptionsProvider)
        {
            IdCache = new MemoryCache(cacheOptions.ToMemoryCacheOptions());
            NameCache = new MemoryCache(cacheOptions.ToMemoryCacheOptions());
        }


        /// <summary>
        /// Stores an object in cache
        /// </summary>
        /// <param name="obj">The object to store</param>
        public void Store(ApiResource obj)
        {
            IdCache.Set(obj.Id, obj, CacheEntryOptions);
        }

        public void Store(NamedApiResource obj)
        {
            // TODO enforce non-nullable name
            if (obj.Name != null)
            {
                NameCache.Set(obj.Name.ToLowerInvariant(), obj, CacheEntryOptions);
            }

            IdCache.Set(obj.Id, obj, CacheEntryOptions);
        }

        public ResourceBase Get(int id) => IdCache.Get<ResourceBase>(id);

        public ResourceBase Get(string name) => NameCache.Get<ResourceBase>(name.ToLowerInvariant());

        public override void Dispose()
        {
            // Ensures that created cache entries are expired
            base.Dispose();
            this.IdCache.Dispose();
            this.NameCache.Dispose();
        }
    }
}
