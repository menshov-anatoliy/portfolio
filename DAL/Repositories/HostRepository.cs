using Molcom.DAL.Entities.Tables.Shared;
using Molcom.DAL.SqlServer.Extensions;
using Molcom.DAL.SqlServer.Repositories.Interfaces;
using NHibernate;
using NHibernate.Linq;

namespace Molcom.DAL.SqlServer.Repositories;

public class HostRepository : Repository<HostDb>, IHostRepository
{
	private const string MultipleHostsFoundFoundMessage = "Ошибка получения хоста. Найдено более одного хоста с кодом \"{0}\". Обратитесь к разрабочикам.";

	public async Task<HostDb> GetOrCreateAsync(ISession session, string hostName)
	{
		var hosts = (await session.Query<HostDb>()
				.Where(h => string.Equals(h.Name, hostName, StringComparison.CurrentCultureIgnoreCase))
				.ToListAsync())
			.ToArray();

		if (hosts.Length > 1)
			throw new ApplicationException(string.Format(MultipleHostsFoundFoundMessage, hostName));

		if (hosts.Length == 1)
			return hosts[0];

		using var transaction = session.BeginTransaction();

		var newHost = new HostDb
		{
			Name = hostName
		};

		await SaveAsync(session, newHost);

		await transaction.CommitIfActiveAsync();

		return newHost;
	}
}

public interface IHostRepository : IReadRepository<HostDb>, IWriteRepository<HostDb>
{
	/// <summary>
	/// Возвращает существующий или создает новый хост с указанным именем
	/// </summary>
	/// <param name="hostName">Имя хоста</param>
	/// <returns>Хост</returns>
	Task<HostDb> GetOrCreateAsync(ISession session, string hostName);
}