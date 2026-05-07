using Microsoft.Extensions.Options;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Domain.Terminal.Models.Forms;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Core.Interfaces.Cache;
using Molcom.Services.Core.Interfaces.Security;
using Molcom.Services.Terminal.Interfaces.Db.Navigations;

namespace Molcom.DAL.Cache.Gateways;

public class TerminalNavigationCachedGateway(IOptions<CachePerTaskOptions> options,
	IMemoryCacheManager cache,
	ITerminalNavigationDbGateway gateway,
	ISecurityContext securityContext) : ITerminalNavigationCachedGateway
{
	public async Task<TerminalForm[]> GetAll(IDbSession session, string task)
	{
		securityContext.ThrowIfNotAuthenticated(out var userId);

		var cacheKey = new GetAllFormsNavigationCacheKey(userId, task);

		var cacheOptions = new MemoryCacheOptions
		{
			SlidingExpiration = options.SlidingExpiration(),
			AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow()
		};

		var forms = (await cache.GetOrCreateAsync(cacheKey, 
			cacheOptions, 
			async () => await gateway.GetAll(session, task)))!;

		// Клонируем объекты для того, чтобы изолировать уровень кэша от вероятных изменений
		return forms
			.Select(f => (TerminalForm)f.Clone())
			.ToArray();
	}

	public void RemoveAllCache(int userId)
	{
		cache.Remove(key => key is GetAllFormsNavigationCacheKey cacheKey && cacheKey.UserId == userId);
	}
}

public class GetAllFormsNavigationCacheKey(int userId, string task) : CacheKey(userId)
{
	public string Task { get; } = task;

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj))
			return false;

		if (ReferenceEquals(this, obj))
			return true;

		if (obj.GetType() != this.GetType())
			return false;

		return UserId == ((GetAllFormsNavigationCacheKey)obj).UserId && Task == ((GetAllFormsNavigationCacheKey)obj).Task;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(UserId, Task);
	}
}