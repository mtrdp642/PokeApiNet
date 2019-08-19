using Newtonsoft.Json;
using PokeApiNet.Cache;
using PokeApiNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PokeApiNet
{
    /// <summary>
    /// Gets data from the PokeAPI service
    /// </summary>
    public class PokeApiClient : IDisposable
    {
        /// <summary>
        /// The default `User-Agent` header value used by instances of <see cref="PokeApiClient"/>.
        /// </summary>
        internal static readonly ProductHeaderValue DefaultUserAgent = GetDefaultUserAgent();

        /// <summary>
        /// The default base address to which requests are to be sent.
        /// </summary>
        internal static readonly Uri DefaultBaseUrl = new Uri("https://pokeapi.co/api/v2/");

        private readonly HttpClient _client;
        private readonly ResourceCacheManager _resourceCache;
        private readonly ResourceListCacheManager _resourceListCache;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PokeApiClient()
            : this(DefaultUserAgent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PokeApiClient"/> with 
        /// a given value for the `User-Agent` header
        /// </summary>
        /// <param name="userAgent">The value for the default `User-Agent` header.</param>
        public PokeApiClient(ProductHeaderValue userAgent)
            : this(DefaultBaseUrl, userAgent, new CacheOptions(), new CacheOptions())
        {
        }

        /// <summary>
        /// Constructor with message handler
        /// </summary>
        /// <param name="messageHandler">Message handler implementation</param>
        public PokeApiClient(HttpMessageHandler messageHandler)
            : this(messageHandler, DefaultUserAgent)
        {
        }

        /// <summary>
        /// Constructor with message handler and `User-Agent` header value
        /// </summary>
        /// <param name="messageHandler">Message handler implementation</param>
        /// <param name="userAgent">The value for the default `User-Agent` header.</param>
        public PokeApiClient(HttpMessageHandler messageHandler, ProductHeaderValue userAgent)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (userAgent == null)
            {
                throw new ArgumentNullException(nameof(userAgent));
            }

            this._client = new HttpClient(messageHandler) { BaseAddress = DefaultBaseUrl };
            this._client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(userAgent));
            this._resourceCache = new ResourceCacheManager(new CacheOptions());
            this._resourceListCache = new ResourceListCacheManager(new CacheOptions());
        }

        internal PokeApiClient(
            Uri baseUrl,
            ProductHeaderValue userAgent,
            CacheOptions resourceCacheOptions,
            CacheOptions resourceListCacheOptions)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            if (userAgent == null)
            {
                throw new ArgumentNullException(nameof(userAgent));
            }

            this._client = new HttpClient { BaseAddress = baseUrl };
            this._client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(userAgent));
            this._resourceCache = new ResourceCacheManager(resourceCacheOptions);
            this._resourceListCache = new ResourceListCacheManager(resourceListCacheOptions);
        }

        /// <summary>
        /// Sets the expiration options for both the resource and resource list cache.
        /// </summary>
        /// <param name="expirationOptions">The expiration options. If null is given, the options are set to default (no expiration).</param>
        public void SetCacheExpirationOptions(CacheExpirationOptions expirationOptions)
        {
            this.SetResourceCacheExpirationOptions(expirationOptions);
            this.SetResourceListCacheExpirationOptions(expirationOptions);
        }

        /// <summary>
        /// Sets the expiration options for the resource cache.
        /// </summary>
        /// <param name="expirationOptions">The expiration options. If null is given, the options are set to default (no expiration).</param>
        public void SetResourceCacheExpirationOptions(CacheExpirationOptions expirationOptions) 
            => this._resourceCache.SetExpirationOptions(expirationOptions);

        /// <summary>
        /// Sets the expiration options for the resource list cache.
        /// </summary>
        /// <param name="expirationOptions">The expiration options. If null is given, the options are set to default (no expiration).</param>
        public void SetResourceListCacheExpirationOptions(CacheExpirationOptions expirationOptions) 
            => this._resourceListCache.SetExpirationOptions(expirationOptions);

        /// <summary>
        /// Close resources
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
            _resourceCache.Dispose();
            _resourceListCache.Dispose();
        }

        private static ProductHeaderValue GetDefaultUserAgent()
        {
            var version = typeof(PokeApiClient).Assembly.GetName().Version;
            return new ProductHeaderValue("PokeApiNet", $"{version.Major}.{version.Minor}");
        }

        /// <summary>
        /// Send a request to the api and serialize the response into the specified type
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="apiParam">The name or id of the resource</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <exception cref="HttpRequestException">Something went wrong with your request</exception>
        /// <returns>An instance of the specified type with data from the request</returns>
        private async Task<T> GetResourcesWithParamsAsync<T>(string apiParam, CancellationToken cancellationToken) where T : ResourceBase
        {
            // lowercase the resource name as the API doesn't recognize upper case and lower case as the same
            string sanitizedApiParam = apiParam.ToLowerInvariant();

            string apiEndpoint = GetApiEndpointString<T>();
            HttpResponseMessage response = await _client.GetAsync($"{apiEndpoint}/{sanitizedApiParam}/", cancellationToken);        // trailing slash is needed!

            response.EnsureSuccessStatusCode();

            string resp = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(resp);
        }

        /// <summary>
        /// Gets a resource from a navigation url; resource is retrieved from cache if possible
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="url">Navigation url</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <exception cref="NotSupportedException">Navigation url doesn't contain the resource id</exception>
        /// <returns>The object of the resource</returns>
        internal async Task<T> GetResourceByUrlAsync<T>(string url, CancellationToken cancellationToken) where T : ResourceBase
        {
            // need to parse out the id in order to check if it's cached.
            // navigation urls always use the id of the resource
            string trimmedUrl = url.TrimEnd('/');
            string resourceId = trimmedUrl.Substring(trimmedUrl.LastIndexOf('/') + 1);

            if (!int.TryParse(resourceId, out int id))
            {
                // not sure what to do here...
                throw new NotSupportedException($"Navigation url '{url}' is in an unexpected format");
            }

            T resource = _resourceCache.Get<T>(id);
            if (resource == null)
            {
                resource = await GetResourcesWithParamsAsync<T>(resourceId, cancellationToken);
                _resourceCache.Store<T>(resource);
            }

            return resource;
        }

        /// <summary>
        /// Gets a resource by id; resource is retrieved from cache if possible
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="id">Id of resource</param>
        /// <returns>The object of the resource</returns>
        public async Task<T> GetResourceAsync<T>(int id) where T : ResourceBase
        {
            return await GetResourceAsync<T>(id, CancellationToken.None);
        }

        /// <summary>
        /// Gets a resource by id; resource is retrieved from cache if possible
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="id">Id of resource</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>The object of the resource</returns>
        public async Task<T> GetResourceAsync<T>(int id, CancellationToken cancellationToken) where T : ResourceBase
        {
            T resource = _resourceCache.Get<T>(id);
            if (resource == null)
            {
                resource = await GetResourcesWithParamsAsync<T>(id.ToString(), cancellationToken);
                _resourceCache.Store(resource);
            }

            return resource;
        }

        /// <summary>
        /// Gets a resource by name; resource is retrieved from cache if possible. This lookup
        /// is case insensitive.
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="name">Name of resource</param>
        /// <returns>The object of the resource</returns>
        public async Task<T> GetResourceAsync<T>(string name) where T : NamedApiResource
        {
            return await GetResourceAsync<T>(name, CancellationToken.None);
        }

        /// <summary>
        /// Gets a resource by name; resource is retrieved from cache if possible. This lookup
        /// is case insensitive.
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="name">Name of resource</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>The object of the resource</returns>
        public async Task<T> GetResourceAsync<T>(string name, CancellationToken cancellationToken) where T : NamedApiResource
        {
            string sanitizedName = name
                .Replace(" ", "-")      // no resource can have a space in the name; API uses -'s in their place
                .Replace("'", "")       // looking at you, Farfetch'd
                .Replace(".", "");      // looking at you, Mime Jr. and Mr. Mime

            // Nidoran is interesting as the API wants 'nidoran-f' or 'nidoran-m'

            T resource = _resourceCache.Get<T>(sanitizedName);
            if (resource == null)
            {
                resource = await GetResourcesWithParamsAsync<T>(sanitizedName, cancellationToken);
                _resourceCache.Store(resource);
            }

            return resource;
        }

        /// <summary>
        /// Resolves all navigation properties in a collection
        /// </summary>
        /// <typeparam name="T">Navigation type</typeparam>
        /// <param name="collection">The collection of navigation objects</param>
        /// <returns>A list of resolved objects</returns>
        public async Task<List<T>> GetResourceAsync<T>(IEnumerable<UrlNavigation<T>> collection) where T : ResourceBase
        {
            return await GetResourceAsync<T>(collection, CancellationToken.None);
        }

        /// <summary>
        /// Resolves all navigation properties in a collection
        /// </summary>
        /// <typeparam name="T">Navigation type</typeparam>
        /// <param name="collection">The collection of navigation objects</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>A list of resolved objects</returns>
        public async Task<List<T>> GetResourceAsync<T>(IEnumerable<UrlNavigation<T>> collection, CancellationToken cancellationToken) where T : ResourceBase
        {
            return (await Task.WhenAll(collection.Select(m => GetResourceAsync(m, cancellationToken)))).ToList();
        }

        /// <summary>
        /// Resolves a single navigation property
        /// </summary>
        /// <typeparam name="T">Navigation type</typeparam>
        /// <param name="urlResource">The single navigation object to resolve</param>
        /// <returns>A resolved object</returns>
        public async Task<T> GetResourceAsync<T>(UrlNavigation<T> urlResource) where T : ResourceBase
        {
            return await GetResourceByUrlAsync<T>(urlResource.Url, CancellationToken.None);
        }

        /// <summary>
        /// Resolves a single navigation property
        /// </summary>
        /// <typeparam name="T">Navigation type</typeparam>
        /// <param name="urlResource">The single navigation object to resolve</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>A resolved object</returns>
        public async Task<T> GetResourceAsync<T>(UrlNavigation<T> urlResource, CancellationToken cancellationToken) where T : ResourceBase
        {
            return await GetResourceByUrlAsync<T>(urlResource.Url, cancellationToken);
        }

        /// <summary>
        /// Clears all cached data for both resources and resource lists
        /// </summary>
        public void ClearCache()
        {
            _resourceCache.ClearAll();
            _resourceListCache.ClearAll();
        }

        /// <summary>
        /// Clears the cached data for a specific resource
        /// </summary>
        /// <typeparam name="T">The type of cache</typeparam>
        public void ClearResourceCache<T>() where T : ResourceBase
        {
            _resourceCache.Clear<T>();
        }

        /// <summary>
        /// Clears the cached data for all resource types
        /// </summary>
        public void ClearResourceCache()
        {
            _resourceCache.ClearAll();
        }

        /// <summary>
        /// Clears the cached data for all resource lists
        /// </summary>
        public void ClearResourceListCache()
        {
            _resourceListCache.ClearAll();
        }

        /// <summary>
        /// Clears the cached data for a specific resource list
        /// </summary>
        /// <typeparam name="T">The type of cache</typeparam>
        public void ClearResourceListCache<T>() where T : ResourceBase
        {
            _resourceListCache.Clear<T>();
        }

        /// <summary>
        /// Gets a single page of named resource data
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>The paged resource object</returns>
        public Task<NamedApiResourceList<T>> GetNamedResourcePageAsync<T>(CancellationToken cancellationToken = default(CancellationToken))
            where T : NamedApiResource
        {
            return GetNamedResourcePageAsync<T>(AddPaginationParamsToUrl(), cancellationToken);
        }

        /// <summary>
        /// Gets the specified page of named resource data
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="limit">The number of resources in a list page</param>
        /// <param name="offset">Page offset</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>The paged resource object</returns>
        public Task<NamedApiResourceList<T>> GetNamedResourcePageAsync<T>(int limit, int offset, CancellationToken cancellationToken = default(CancellationToken))
            where T : NamedApiResource
        {
            return GetNamedResourcePageAsync<T>(AddPaginationParamsToUrl(limit, offset), cancellationToken);
        }

        /// <summary>
        /// Handles cache manipulation
        /// </summary>
        private async Task<NamedApiResourceList<T>> GetNamedResourcePageAsync<T>(Func<string, string> urlFn, CancellationToken cancellationToken)
            where T : NamedApiResource
        {
            string pageUrl = urlFn(GetApiEndpointString<T>());
            NamedApiResourceList<T> resources = _resourceListCache.GetNamedResourceList<T>(pageUrl);
            if (resources == null)
            {
                resources = await GetPageAsync(JsonConvert.DeserializeObject<NamedApiResourceList<T>>, cancellationToken)(pageUrl) as NamedApiResourceList<T>;
                _resourceListCache.Store(pageUrl, resources);
            }
            return resources;
        }

        /// <summary>
        /// Gets a single page of unnamed resource data
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>The paged resource object</returns>
        public Task<ApiResourceList<T>> GetApiResourcePageAsync<T>(CancellationToken cancellationToken = default(CancellationToken))
            where T : ApiResource
        {
            return GetApiResourcePageAsync<T>(AddPaginationParamsToUrl(), cancellationToken);
        }

        /// <summary>
        /// Gets the specified page of unnamed resource data
        /// </summary>
        /// <typeparam name="T">The type of resource</typeparam>
        /// <param name="limit">The number of resources in a list page</param>
        /// <param name="offset">Page offset</param>
        /// <param name="cancellationToken">Cancellation token for the request; not utilitized if data has been cached</param>
        /// <returns>The paged resource object</returns>
        public Task<ApiResourceList<T>> GetApiResourcePageAsync<T>(int limit, int offset, CancellationToken cancellationToken = default(CancellationToken))
            where T : ApiResource
        {
            return GetApiResourcePageAsync<T>(AddPaginationParamsToUrl(limit, offset), cancellationToken);
        }

        /// <summary>
        /// Handles cache manipulation
        /// </summary>
        private async Task<ApiResourceList<T>> GetApiResourcePageAsync<T>(Func<string, string> urlFn, CancellationToken cancellationToken)
            where T : ApiResource
        {
            string pageUrl = urlFn(GetApiEndpointString<T>());
            ApiResourceList<T> resources = _resourceListCache.GetApiResourceList<T>(pageUrl);
            if(resources == null)
            {
                resources = await GetPageAsync(JsonConvert.DeserializeObject<ApiResourceList<T>>, cancellationToken)(pageUrl) as ApiResourceList<T>;
                _resourceListCache.Store(pageUrl, resources);
            }
            return resources;
        }

        /// <summary>
        /// Handles fetch and deserialization of a resource page
        /// </summary>
        private Func<string, Task<ResourceList<T>>> GetPageAsync<T>(Func<string, ResourceList<T>> deserializeFn, CancellationToken cancellationToken)
            where T : ResourceBase
        {
            return async pageUrl =>
            {
                HttpResponseMessage response = await _client.GetAsync(pageUrl, cancellationToken);

                response.EnsureSuccessStatusCode();

                return deserializeFn(await response.Content.ReadAsStringAsync());
            };
        }

        private static Func<string, string> AddPaginationParamsToUrl(int? limit = null, int? offset = null)
        {
            Dictionary<string, string> queryParameters = new Dictionary<string, string>();

            // TODO consider to always set the limit parameter when not present to the default "20"
            // in order to have a single cached resource list for requests with explicit or implicit default limit
            if (limit.HasValue)
            {
                queryParameters.Add(nameof(limit), limit.Value.ToString());
            }
            if (offset.HasValue)
            {
                queryParameters.Add(nameof(offset), offset.Value.ToString());
            }
            return uri => QueryHelpers.AddQueryString(uri, queryParameters);
        }

        private static string GetApiEndpointString<T>()
        {
            PropertyInfo propertyInfo = typeof(T).GetProperty("ApiEndpoint", BindingFlags.Static | BindingFlags.NonPublic);
            return propertyInfo.GetValue(null).ToString();
        }
    }
}
