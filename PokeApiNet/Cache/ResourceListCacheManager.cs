using PokeApiNet.Models;
using System;

namespace PokeApiNet.Cache
{
    /// <summary>
    /// Manages caches for instances of subclasses from <see cref="ResourceList{T}"/>
    /// </summary>
    internal sealed class ResourceListCacheManager : BaseCacheManager<ExpirableResourceListCache>
    {
        public ResourceListCacheManager(CacheOptions cacheOptions)
            : base(cacheOptions)
        {
        }

        /// <summary>
        /// Caches a value
        /// </summary>
        /// <typeparam name="T">Type of object to be cached</typeparam>
        /// <param name="url">The URL associated to the resource list</param>
        /// <param name="obj">The resource list to cache</param>
        /// <exception cref="NotSupportedException">
        /// The given type is not supported for searching via PokeAPI
        /// </exception>
        public void Store<T>(string url, ResourceList<T> obj)
            where T : ResourceBase
        {
            System.Type resourceType = typeof(T);
            if (!IsTypeSupported(resourceType))
            {
                throw new NotSupportedException($"{resourceType.FullName} is not supported.");
            }

            Cache[resourceType].Store(url, obj);
        }

        /// <summary>
        /// Gets the <see cref="ApiResourceList{T}"/> stored in cache associated to a URL.
        /// </summary>
        /// <typeparam name="T">The type of resource in the resource list.</typeparam>
        /// <param name="url">The URL to access the cache with.</param>
        /// <returns>
        /// A <see cref="ApiResourceList{T}"/> in case of a cache hit; null otherwise.
        /// </returns>
        public ApiResourceList<T> GetApiResourceList<T>(string url) where T : ApiResource
        {
            System.Type resourceType = typeof(T);
            return Cache[resourceType].Get<T>(url) as ApiResourceList<T>;
        }

        /// <summary>
        /// Gets the <see cref="NamedApiResourceList{T}"/> stored in cache associated to a URL.
        /// </summary>
        /// <typeparam name="T">The type of resource in the resource list.</typeparam>
        /// <param name="url">The URL to access the cache with.</param>
        /// <returns>
        /// A <see cref="NamedApiResourceList{T}"/> in case of a cache hit; null otherwise.
        /// </returns>
        public NamedApiResourceList<T> GetNamedResourceList<T>(string url) where T : NamedApiResource
        {
            System.Type resourceType = typeof(T);
            return Cache[resourceType].Get<T>(url) as NamedApiResourceList<T>;
        }

        protected override ExpirableResourceListCache CreateCacheForResource(CacheOptions cacheOptions)
            => new ExpirableResourceListCache(cacheOptions, this.ExpirationOptionsChanges);
    }
}
