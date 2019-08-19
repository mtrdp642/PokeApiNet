using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;

namespace PokeApiNet.Cache
{
    internal abstract class BaseExpirableCache : IDisposable
    {
        private readonly IDisposable expirationOptionsSub;
        private CancellationTokenSource clearToken = new CancellationTokenSource();
        private DateTimeOffset? absoluteExpiration;
        private TimeSpan? absoluteExpirationRelativeToNow;
        private TimeSpan? slidingExpiration;

        public BaseExpirableCache(IObservable<CacheExpirationOptions> expirationOptionsProvider)
        {
            this.expirationOptionsSub = expirationOptionsProvider.Subscribe(new CacheExpirationOptionsObserver(this));
        }

        /// <summary>
        /// Gets a <see cref="MemoryCacheEntryOptions"/> instance.
        /// </summary>
        /// <remarks>
        /// New options instance has to be constantly instantiated instead of shared
        /// as a consequence of <see cref="clearToken"/> being mutable
        /// </remarks>
        protected MemoryCacheEntryOptions CacheEntryOptions
        {
            get
            {
                var opts = new MemoryCacheEntryOptions()
                    .AddExpirationToken(new CancellationChangeToken(clearToken.Token))
                    .SetSize(1);
                opts.AbsoluteExpiration = this.absoluteExpiration;
                opts.AbsoluteExpirationRelativeToNow = this.absoluteExpirationRelativeToNow;
                opts.SlidingExpiration = this.slidingExpiration;
                return opts;
            }
        }

        /// <summary>
        /// Triggers eviction because of expiration of all cache entries.
        /// </summary>
        public void TriggerEviction()
        {
            // TODO add lock?
            if (clearToken != null && !clearToken.IsCancellationRequested && clearToken.Token.CanBeCanceled)
            {
                clearToken.Cancel();
                clearToken.Dispose();
            }

            clearToken = new CancellationTokenSource();
        }

        public virtual void Dispose()
        {
            this.TriggerEviction();
            this.expirationOptionsSub.Dispose();
        }

        private void UpdateCacheExpirationOptions(CacheExpirationOptions expirationOptions)
        {
            this.absoluteExpiration = expirationOptions.AbsoluteExpiration;
            this.absoluteExpirationRelativeToNow = expirationOptions.AbsoluteExpirationRelativeToNow;
            this.slidingExpiration = expirationOptions.SlidingExpiration;
        }

        private sealed class CacheExpirationOptionsObserver : IObserver<CacheExpirationOptions>
        {
            private readonly BaseExpirableCache cache;

            public CacheExpirationOptionsObserver(BaseExpirableCache cache)
            {
                this.cache = cache;
            }

            public void OnCompleted()
            {
                // NOOP
            }

            public void OnError(Exception error)
            {
                // NOOP
            }

            public void OnNext(CacheExpirationOptions value)
            {
                this.cache.UpdateCacheExpirationOptions(value);
            }
        }
    }
}
