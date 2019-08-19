using PokeApiNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PokeApiNet.Cache
{
    /// <summary>
    /// Base class for a cache manager implementation.
    /// </summary>
    internal abstract class BaseCacheManager<TExpirableCache> : IDisposable
        where TExpirableCache : BaseExpirableCache
    {
        protected static readonly IReadOnlyCollection<System.Type> ResourceTypes;
        private readonly Dictionary<System.Type, TExpirableCache> resourceTypeToCache;
        private readonly CacheExpirationOptionsSource expirationOptionsSource;

        static BaseCacheManager()
        {
            ResourceTypes = new HashSet<System.Type>(GetResourceTypes());
        }

        public BaseCacheManager(CacheOptions cacheOptions)
        {
            this.expirationOptionsSource = new CacheExpirationOptionsSource();
            this.resourceTypeToCache = ResourceTypes.ToDictionary(x => x, _ => this.CreateCacheForResource(cacheOptions));
        }

        public IObservable<CacheExpirationOptions> ExpirationOptionsChanges => this.expirationOptionsSource;

        public IReadOnlyCollection<System.Type> CachedTypes => this.Cache.Keys.ToList();

        protected IReadOnlyDictionary<System.Type, TExpirableCache> Cache => this.resourceTypeToCache;

        /// <summary>
        /// Sets the expiration options for the cache entries.
        /// </summary>
        /// <param name="expirationOptions">The expiration options. If null is given, the options are set to default (no expiration).</param>
        public void SetExpirationOptions(CacheExpirationOptions expirationOptions)
            => this.expirationOptionsSource.UpdateOptions(expirationOptions ?? new CacheExpirationOptions());

        /// <summary>
        /// Clears all caches
        /// </summary>
        public void ClearAll()
        {
            foreach (BaseExpirableCache cache in Cache.Values)
            {
                cache.TriggerEviction();
            }
        }

        /// <summary>
        /// Clears a specific cache
        /// </summary>
        /// <typeparam name="T">The type of cache to clear</typeparam>
        public void Clear<T>() where T : ResourceBase
        {
            System.Type type = typeof(T);
            Cache[type].TriggerEviction();
        }

        public void Dispose()
        {
            this.expirationOptionsSource.CloseStream();
            foreach (TExpirableCache cache in this.resourceTypeToCache.Values)
            {
                cache.Dispose();
            }
            resourceTypeToCache.Clear();
        }

        protected bool IsTypeSupported(System.Type type) => ResourceTypes.Contains(type);

        protected abstract TExpirableCache CreateCacheForResource(CacheOptions cacheOptions);

        private static IEnumerable<System.Type> GetResourceTypes()
            => Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ResourceBase)));
    }
}
