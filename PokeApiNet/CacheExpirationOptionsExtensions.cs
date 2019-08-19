using System;

namespace PokeApiNet
{
    /// <summary>
    /// Companion class for <see cref="CacheExpirationOptions"/> which provides commodity fluent methods.
    /// </summary>
    public static class CacheExpirationOptionsExtensions
    {
        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="options">The <see cref="CacheExpirationOptions"/>.</param>
        /// <param name="relative">The expiration time, relative to now.</param>
        /// <returns>The <see cref="CacheExpirationOptions"/> so that additional calls can be chained.</returns>
        public static CacheExpirationOptions SetAbsoluteExpiration(
            this CacheExpirationOptions options,
            TimeSpan relative)
        {
            options.AbsoluteExpirationRelativeToNow = relative;
            return options;
        }

        /// <summary>
        /// Sets an absolute expiration date for the cache entry.
        /// </summary>
        /// <param name="options">The <see cref="CacheExpirationOptions"/>.</param>
        /// <param name="absolute">The expiration time, in absolute terms.</param>
        /// <returns>The <see cref="CacheExpirationOptions"/> so that additional calls can be chained.</returns>
        public static CacheExpirationOptions SetAbsoluteExpiration(
            this CacheExpirationOptions options,
            DateTimeOffset absolute)
        {
            options.AbsoluteExpiration = absolute;
            return options;
        }

        /// <summary>
        /// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="options">The <see cref="CacheExpirationOptions"/>.</param>
        /// <param name="offset">The sliding expiration time.</param>
        /// <returns>The <see cref="CacheExpirationOptions"/> so that additional calls can be chained.</returns>
        public static CacheExpirationOptions SetSlidingExpiration(
            this CacheExpirationOptions options,
            TimeSpan offset)
        {
            options.SlidingExpiration = offset;
            return options;
        }
    }
}
