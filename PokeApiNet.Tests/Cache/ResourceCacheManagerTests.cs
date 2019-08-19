using FluentAssertions;
using NSubstitute;
using PokeApiNet.Cache;
using PokeApiNet.Models;
using System;
using Xunit;

namespace PokeApiNet.Tests.Cache
{
    public class ResourceCacheManagerTests
    {
        private readonly CacheOptions cacheOptions;

        public ResourceCacheManagerTests()
        {
            this.cacheOptions = new CacheOptions();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_StoredId_ReturnsResource()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "cheri", Id = 1 };
            sut.Store(berry);

            // act
            Berry retrievedBerry = sut.Get<Berry>(berry.Id);

            // assert
            Assert.Same(berry, retrievedBerry);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_NonStoredId_ReturnsNull()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "cheri", Id = 1 };
            sut.Store(berry);

            // act
            Berry retrievedBerry = sut.Get<Berry>(2);

            // assert
            Assert.Null(retrievedBerry);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void StoreThrowsIfTypeNotSupported()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            TestClass test = new TestClass { Id = 1 };

            // assert
            Assert.Throws<NotSupportedException>(() =>
            {
                // act
                sut.Store(test);
            });
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_ByIdOnEmptyCache_ReturnsNull()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();

            // act
            Pokedex retrievedPokedex = sut.Get<Pokedex>(1);

            // assert
            Assert.Null(retrievedPokedex);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_ByNameOnEmptyCache_ReturnsNull()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();

            // act
            Pokemon retrievedPokemon = sut.Get<Pokemon>("pikachu");

            // assert
            Assert.Null(retrievedPokemon);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_StoredName_ReturnsResource()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "cheri", Id = 1 };
            sut.Store(berry);

            // act
            Berry retrievedBerry = sut.Get<Berry>("cheri");

            // assert
            Assert.Same(berry, retrievedBerry);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_NonStoredName_ReturnsNull()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "cheri", Id = 1 };
            sut.Store(berry);

            // act
            Berry retrievedBerry = sut.Get<Berry>("abc123");

            // assert
            Assert.Null(retrievedBerry);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void AllCacheIsCleared()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "cheri", Id = 1 };
            Pokedex pokedex = new Pokedex { Name = "dex", Id = 1 };
            sut.Store(berry);
            sut.Store(pokedex);

            // act
            sut.ClearAll();

            // assert
            Berry retrievedBerry = sut.Get<Berry>(1);
            Pokedex retrievedPokedex = sut.Get<Pokedex>(1);
            Assert.Null(retrievedBerry);
            Assert.Null(retrievedPokedex);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void CacheIsClearedForSpecificType()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "cheri", Id = 1 };
            Pokedex pokedex = new Pokedex { Name = "cheri", Id = 1 };
            sut.Store(berry);
            sut.Store(pokedex);

            // act
            sut.Clear<Berry>();

            // assert
            Berry retrievedBerry = sut.Get<Berry>(1);
            Pokedex retrievedPokedex = sut.Get<Pokedex>(1);
            Assert.Null(retrievedBerry);
            Assert.NotNull(retrievedPokedex);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Get_StoredNameWithDifferentCasing_ReturnsResource()
        {
            // assemble
            ResourceCacheManager sut = CreateSut();
            Berry berry = new Berry { Name = "CHERI" };
            sut.Store(berry);

            // act
            Berry retrievedBerry = sut.Get<Berry>("cheri");

            // assert
            Assert.Same(berry, retrievedBerry);
        }

        [Fact]
        public void CorrectlyDisposes()
        {
            IObserver<CacheExpirationOptions> fakeObserver = Substitute.For<IObserver<CacheExpirationOptions>>();
            ResourceCacheManager sut = CreateSut();
            sut.ExpirationOptionsChanges.Subscribe(fakeObserver);

            sut.Dispose();

            fakeObserver.Received().OnCompleted();
            sut.CachedTypes.Should().BeEmpty();
        }

        private ResourceCacheManager CreateSut()
        {
            return new ResourceCacheManager(this.cacheOptions);
        }

        private sealed class TestClass : ResourceBase
        {
            public override int Id { get; set; } = 1;

            public new static string ApiEndpoint => "";
        };
    }
}
