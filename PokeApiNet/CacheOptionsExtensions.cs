using Microsoft.Extensions.Caching.Memory;

namespace PokeApiNet
{
    /// <summary>
    /// Companion class for <see cref="CacheOptions"/> that provides extensions methods.
    /// </summary>
    internal static class CacheOptionsExtensions
    {
        internal static MemoryCacheOptions ToMemoryCacheOptions(this CacheOptions source)
        {
            return new MemoryCacheOptions
            {
                CompactionPercentage = source.CompactionPercentage,
                SizeLimit = source.SizeLimit
            };
        }
    }
}
