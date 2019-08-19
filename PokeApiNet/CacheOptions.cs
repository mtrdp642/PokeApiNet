using System;

namespace PokeApiNet
{
    /// <summary>
    /// Represents the configuration of a cache.
    /// </summary>
    public class CacheOptions
    {
        private long? _sizeLimit;
        private double _compactionPercentage = 0.05;

        /// <summary>
        /// Gets or sets the maximum number of entries in the cache.
        /// </summary>
        public long? SizeLimit
        {
            get => _sizeLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                _sizeLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the amount to compact the cache by when the maximum size is exceeded.
        /// </summary>
        public double CompactionPercentage
        {
            get => _compactionPercentage;
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be between 0 and 1 inclusive.");
                }

                _compactionPercentage = value;
            }
        }
    }
}
