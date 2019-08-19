# PokeApiNet
A .Net wrapper for the Pokemon API at [https://pokeapi.co](https://pokeapi.co).

Targets .Net Standard 2.0.

[![NuGet](https://img.shields.io/nuget/v/PokeApiNet.svg?logo=nuget)](https://www.nuget.org/packages/PokeApiNet)
[![Build Status](https://mtrdp642.visualstudio.com/PokeApiNet/_apis/build/status/mtrdp642.PokeApiNet?branchName=master)](https://mtrdp642.visualstudio.com/PokeApiNet/_build/latest?definitionId=1&branchName=master)

# Use
```cs
using PokeApiNet;
using PokeApiNet.Models;

...

// instantiate client
PokeApiClient pokeClient = new PokeApiClient();

// get a resource by name
Pokemon hoOh = await pokeClient.GetResourceAsync<Pokemon>("ho-oh");

// ... or by id
Item clawFossil = await pokeClient.GetResourceAsync<Item>(100);
```

To see all the resources that are available, see the [PokeAPI docs site](https://pokeapi.co/docs/v2.html).

## Navigation URLs
PokeAPI uses navigation urls for many of the resource's properties to keep requests lightweight, but require subsequent requests in order to resolve this data. Example:
```cs
Pokemon pikachu = await pokeClient.GetResourceAsync<Pokemon>("pikachu");
```

`pikachu.Species` only has a `Name` and `Url` property. In order to load this data, an additonal request is needed; this is more of a problem when the property is a list of navigation URLs, such as the `pikachu.Moves.Move` collection.

`GetResourceAsync` includes overloads to assist with resolving these navigation properties. Example:
```cs
// to resolve a single navigation url property
PokemonSpecies species = await pokeClient.GetResourceAsync(pikachu.Species);

// to resolve a list of them
List<Move> allMoves = await pokeClient.GetResourceAsync(pikachu.Moves.Select(move => move.Move));
```

## Paging
PokeAPI supports the paging of resources, allowing users to get a list of available resources for that API. Depending on the shape of the resource data, two methods for paging are included, along with overloads to allow for the specification of the page count limit and the page offset. Example:
```cs
// get a page of data (defaults to a limit of 20)
NamedApiResourceList<Berry> firstBerryPage = await client.GetNamedResourcePageAsync<Berry>();

// to specify a certain page, use the provided overloads
NamedApiResourceList<Berry> lotsMoreBerriesPage = await client.GetNamedResourcePageAsync<Berry>(60, 2);
```

Because the `Berry` resource has a `Name` property, the `GetNamedResourcePageAsync()` method is used. For resources that do not have a `Name` property, use the `GetApiResourcePageAsync()` method. Example:
```cs
ApiResourceList<ContestEffect> contestEffectPage = await client.GetApiResourcePageAsync<ContestEffect>();
```

Regardless of which method is used, the returning object includes a `Results` collection that can be used to pull the full resource data. Example:
```cs
Berry cheri = await client.GetResourceAsync<Berry>(firstBerryPage.Results[0]);
```

Refer to the PokeAPI documention to see which resources include a `Name` property.

## Caching
Every resource and page response is automatically cached in memory, making all subsequent requests for the same resource or page pull cached data. Example:
```cs
// this will fetch the data from the API
Pokemon mew = await pokeClient.GetResourceAsync<Pokemon>(151);

// another call to the same resource will fetch from the cache
Pokemon mewCached = await pokeClient.GetResourceAsync<Pokemon>("mew");
```

To clear the cache of data:
```cs
// clear all caches for both resources and pages
pokeClient.ClearCache();
```

Additional overloads are provided to allow for clearing the individual caches for resources and pages, as well as by type of cache.

### Cache expiration

By default, a `PokeApiClient` instance neither sets limits to how many cache entries can be stored nor any form of expiration policy.
Memory is a scarce resource, so consumers should try to limit the cache growth over time by using expiration policies.

The client API allows this through the `SetCacheExpirationOptions`, `SetResourceCacheExpirationOptions`and `SetResourceListCacheExpirationOptions` methods.
Its strongly adviced to at least set a _sliding expiration_ value. 

```cs
PokeApiClient client = new PokeApiClient();
client.SetCacheExpirationOptions(new CacheExpirationOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
```

# Build
```
dotnet build
```

# Test
The test bank includes unit tests and integration tests. Integration tests will actually ping the PokeApi server for data while the unit tests run off of mocked data.
```
dotnet test --filter "Category = Unit"
dotnet test --filter "Category = Integration"
```
