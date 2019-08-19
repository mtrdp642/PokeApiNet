using PokeApiNet.Models;
using System;

namespace PokeApiNet.Cache
{
    /// <summary>
    /// Manages caches for instances of subclasses from <see cref="ResourceBase"/>
    /// </summary>
    internal sealed class ResourceCacheManager : BaseCacheManager<ExpirableResourceCache>
    {
        public ResourceCacheManager(CacheOptions cacheOptions)
            : base(cacheOptions)
        {
        }

        /// <summary>
        /// Caches a value
        /// </summary>
        /// <typeparam name="T">Type of object to be cached</typeparam>
        /// <param name="obj">Object to cache</param>
        /// <exception cref="NotSupportedException">
        /// The given type is not supported for searching via PokeAPI
        /// </exception>
        public void Store<T>(T obj) where T : ResourceBase
        {
            System.Type resourceType = typeof(T);
            if (!IsTypeSupported(resourceType))
            {
                throw new NotSupportedException($"{resourceType.FullName} is not supported.");
            }

            // Defer type inference to runtime, so that the correct
            // overload of Store is invoked
            Cache[resourceType].Store(obj as dynamic);
        }

        /// <summary>
        /// Gets a value from cache by id or null if not cached or if not found
        /// </summary>
        /// <typeparam name="T">Type of object to get</typeparam>
        /// <param name="id">Id of the resource</param>
        /// <returns>The cached object or null if not found</returns>
        public T Get<T>(int id) where T : ResourceBase
        {
            System.Type resourceType = typeof(T);
            return Cache[resourceType].Get(id) as T;
        }

        /// <summary>
        /// Gets a value from cache by name or null if not cached or if not found
        /// </summary>
        /// <typeparam name="T">Type of object to get</typeparam>
        /// <param name="name">Name of the resource</param>
        /// <returns>The cached object or null if not found</returns>
        public T Get<T>(string name) where T : NamedApiResource
        {
            System.Type resourceType = typeof(T);
            return Cache[resourceType].Get(name) as T;
        }

        protected override ExpirableResourceCache CreateCacheForResource(CacheOptions cacheOptions)
            => new ExpirableResourceCache(cacheOptions, this.ExpirationOptionsChanges);
    }
}