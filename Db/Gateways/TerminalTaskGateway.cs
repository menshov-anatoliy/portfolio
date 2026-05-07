using Microsoft.Extensions.Options;
using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Domain.Terminal.Models;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Core.Interfaces.Security;
using Molcom.Services.Terminal.Interfaces.Db;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using System.Linq;
using Molcom.DAL.Entities.Tables.Terminals.Navigations;
using Molcom.DAL.Entities.Tables.Terminals.Rights;
using Molcom.DAL.Entities.Tables.Terminals.Users;

namespace Molcom.DAL.SqlServer.Gateways.Terminals;

public class TerminalTaskGateway(IOptions<MocksOptions> mockOptions,
	IOptions<HotKeysOptions> hotkeyOptions,
	ISecurityContext securityContext) : BaseGateway(mockOptions), ITerminalTaskDbGateway
{
	public async Task<TerminalTask[]> GetAll(IDbSession session, int[] mandants)
	{
		var internalSession = (ISession)session.InternalSession;

		return await GetAll(internalSession, mandants);
	}

	private async Task<TerminalTask[]> GetAll(ISession session, int[] mandants)
	{
		var allAvailableTasks = (await session.Query<UserToGroupDb>()
				.Where(u2g => u2g.User.Id == securityContext.UserId)
				.Where(u2g => u2g.Mandant == null || mandants.Contains(u2g.Mandant.Id))
				.Join(session.Query<RightsToTermDb>(),
					u2g => u2g.UserGroup.Code,
					r2t => r2t.UserGroup.Code,
					(u2g, r2t) => r2t)
				.Join(session.Query<TerminalTaskToMandantDb>(),
					r2t => r2t.Terminal.Id,
					t2m => t2m.Task.Id,
					(r2t, t2m) => t2m)
				.Where(t2m => mandants.Contains(t2m.Mandant.Id))
				.Select(t2m => t2m.Task)
				.Distinct()
				.ToListAsync())
			.ToArray();

		var allFolders = (await session.Query<TerminalFolderDb>()
				.ToListAsync())
			.ToArray();

		var availableFolders = new List<TerminalFolderDb>();

		// Выбираем только те папки, которые с содержимым
		var notEmptyFolders = allAvailableTasks
			.Where(t => t.Folder != null)
			.Select(t => t.Folder)
			.DistinctBy(f => f!.Id)
			.ToArray();

		availableFolders.AddRange(notEmptyFolders!);

		var currentLevelFolders = notEmptyFolders;

		TerminalFolderDb[] nextLevelFolders;

		// Находим всю цепочку родительских папок для непустых папок
		do
		{
			nextLevelFolders = allFolders
				.Where(f => currentLevelFolders.Any(cf => cf.Parent?.Id == f.Id))
				.ToArray();

			// Добавляем папки нового уровня, исключая уже добавленные
			availableFolders.AddRange(nextLevelFolders.Where(nlf => availableFolders.All(f => nlf.Id != f.Id)));

			currentLevelFolders = nextLevelFolders;

		} while (nextLevelFolders.Length > 0);

		var availableHotKeysForMainMenu = hotkeyOptions.Value.AvailableForMainMenu;

		var folders = HierarchyExtensions.CreateTree(availableFolders.ToArray(),
				dto => dto.Id,
				dto => dto.Parent?.Id,
				(current, child) => current.AppendChild(child),
				folder =>
				{
					var terminalTask = new FolderTerminalTask(folder.Id,
						folder.Name,
						folder.Name,
						folder.Order);

					var children = allAvailableTasks
						.Where(t => t.Folder == folder)
						.Select(t => new ItemTerminalTask(t.Id,
							t.Define.Code,
							t.Name,
							t.Order,
							t.HotKey,
							availableHotKeysForMainMenu))
						.ToArray();

					terminalTask.AppendChildren(children);

					return terminalTask;
				}).Cast<TerminalTask>()
			.ToArray();

		var tasks = allAvailableTasks
			.Where(t => t.Folder == null)
			.Select(t => new ItemTerminalTask(t.Id,
				t.Define.Code,
				t.Name,
				t.Order,
				t.HotKey,
				availableHotKeysForMainMenu))
			.Cast<TerminalTask>()
			.ToArray();

		return tasks
			.Union(folders)
			.ToArray();
	}
}
