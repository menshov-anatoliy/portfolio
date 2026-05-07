using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using Molcom.Services.Core.Interfaces.Cache;
using Molcom.Services.Core.Interfaces.Cache.LifeTimes;
using MemoryCacheOptions = Molcom.Services.Core.Interfaces.Cache.MemoryCacheOptions;

namespace Molcom.DAL.Cache;

public class MemoryCacheManager(
	IMemoryCache memoryCache,
	IServiceProvider provider) : IMemoryCacheManager
{
	public async Task<TItem?> GetOrCreateAsync<TItem>(CacheKey key, MemoryCacheOptions cacheOptions, Func<Task<TItem>> factory)
	{
		if (memoryCache.TryGetValue(key, out var result))
			return (TItem?)result;

		using var entry = memoryCache.CreateEntry(key);

		entry.AbsoluteExpiration = cacheOptions.AbsoluteExpiration;

		entry.AbsoluteExpirationRelativeToNow = cacheOptions.AbsoluteExpirationRelativeToNow;

		entry.SlidingExpiration = cacheOptions.SlidingExpiration;

		result = await factory().ConfigureAwait(false);

		entry.Value = result;

		return (TItem?)result;
	}

	public void Remove(CacheKey key)
	{
		memoryCache.Remove(key);
	}

	public void Remove(Predicate<CacheKey> predicate)
	{
		var allKeys = AllKeys();

		var predicatedKeys = allKeys
			.Where(k => predicate(k))
			.ToArray();

		foreach (var key in predicatedKeys)
		{
			memoryCache.Remove(key);
		}
	}

	public void RemoveAllServicesCache<T>(int userId) where T : ICachedGateway
	{
		var cachedServices = CachedServices<T>();

		foreach (var cachedService in cachedServices)
		{
			cachedService.RemoveAllCache(userId);
		}
	}

	private T[] CachedServices<T>() where T : ICachedGateway
	{
		var interfaceType = typeof(ICachedGateway);

		var entryAssembly = Assembly.GetEntryAssembly()!;

		var allCachedInterfaceTypes = entryAssembly
			.GetReferencedAssemblies()
			.Select(Assembly.Load)
			.SelectMany(x => x.GetTypes())
			.Where(interfaceType.IsAssignableFrom)
			.Where(type => type.IsInterface)
			.ToArray();

		if (allCachedInterfaceTypes.Length == 0)
			return [];

		using var scope = provider.CreateAsyncScope();

		var serviceProvider = scope.ServiceProvider;

		return allCachedInterfaceTypes
			.SelectMany(serviceProvider.GetServices)
			.OfType<T>()
			.ToArray();
	}

	/// <summary>
	/// Реализация для версии .NET 8.0.10 - October 08, 2024
	/// https://github.com/dotnet/core/blob/main/release-notes/8.0/8.0.10/8.0.10.md
	/// https://stackoverflow.com/questions/45597057/how-to-retrieve-a-list-of-memory-cache-keys-in-asp-net-core
	/// </summary>
	/// <returns></returns>
	/// <exception cref="ApplicationException"></exception>
	private CacheKey[] AllKeys()
	{
		if (memoryCache is MemoryCache cache)
			return cache.Keys
				.OfType<CacheKey>()
				.ToArray();

		var coherentState = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);

		var coherentStateValue = coherentState!.GetValue(memoryCache);

		var stringEntriesCollection = coherentStateValue!.GetType().GetProperty("NonStringEntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);

		var stringEntriesCollectionValue = stringEntriesCollection!.GetValue(coherentStateValue) as ICollection;

		var keys = new List<CacheKey>();

		if (stringEntriesCollectionValue != null)
		{
			foreach (var item in stringEntriesCollectionValue)
			{
				var methodInfo = item.GetType().GetProperty("Key");

				var value = methodInfo!.GetValue(item);

				if (value is not CacheKey cacheKey)
					throw new ApplicationException("Ошибка работы менеджера кэша. Ключ кэша не соотвествует системному типу. Обратитесь кразработчикам.");

				keys.Add(cacheKey);
			}
		}

		return keys.ToArray();
	}
}