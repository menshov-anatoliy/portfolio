using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces.Services;
using Molcom.Domain.Shared.Models.Sessions;
using Molcom.Domain.Terminal.Interfaces.Services;
using Molcom.Domain.Terminal.Interfaces.Services.Sessions;
using Molcom.Domain.Terminal.Models;
using Molcom.Services.Core.Interfaces.Security;
using Molcom.Services.Core.Services;
using Molcom.Services.Terminal.Interfaces.Security;
using Molcom.Shared.Facade.Services;
using Molcom.Terminal.Facade.Models;
using Molcom.Terminal.Facade.Models.Forms;
using Molcom.Terminal.Facade.Models.Tasks;
using Molcom.Terminal.Facade.Services.Interfaces;

namespace Molcom.Terminal.Facade.Services
{
	public class TerminalSessionFacadeService(
		ITerminalSecurityContext securityContext,
		ITaskTerminalService taskService,
		IMandantService mandantService,
		IAuthenticationService authenticationService,
		IApplicationSessionTerminalService applicationSessionService,
		ITaskSessionTerminalService taskSessionService)
		: FacadeService, ITerminalSessionFacadeService
	{
		public async Task<TerminalTaskItemDTO[]> GetTasks()
		{
			securityContext.ThrowIfNotAuthenticated(out var userId);

			var applicationSession = await applicationSessionService.GetActiveApplicationSessionByUserId(userId);

			applicationSession.ThrowIfNotInitialized();

			var tasks = await taskService.GetAllTasks(applicationSession.ActiveMandants());

			var dtos = HierarchyExtensions.ConvertTree(tasks,
					obj => obj.Id,
					obj => obj.Folder != null ? obj.Folder!.Id : null,
					obj =>
					{
						if (obj.Type == TerminalTaskTypesEnum.Item)
							return [];

						return ((FolderTerminalTask)obj).Items;
					},
					(to, what) => to.AppendChild(what),
					obj => new TerminalTaskItemDTO
					{
						Id = obj.Id,
						Type = MappingExtensions.Map(obj.Type, value =>
						{
							if (value == TerminalTaskTypesEnum.Item)
								return TerminalTaskItemTypesDTOEnum.Item;

							if (value == TerminalTaskTypesEnum.Folder)
								return TerminalTaskItemTypesDTOEnum.Folder;

							throw new ApplicationException(
								$"Не найден метод преобразования для типа {nameof(TerminalTaskTypesEnum)}. Обратитесь к разработчикам.");
						}),
						Caption = obj.Caption,
						Code = obj.Code,
						Order = obj.Order,
						HotKey = obj.HotKey
					},
					default(TerminalMenuItemDTO))
				.OrderTasks()
				.ToArray();

			return dtos;
		}

		public async Task<bool> HasActiveApplicationSession(int sessionId)
		{
			var sessionResponse = await applicationSessionService.GetActiveApplicationSession(sessionId);

			if (sessionResponse == null)
				return false;

			return true;
		}

		public async Task CloseApplicationSession(int sessionId)
		{
			await applicationSessionService.CloseApplicationSession(sessionId);
		}

		public async Task UpdateApplicationSessionLastActivityDate(int sessionId)
		{
			await applicationSessionService.UpdateApplicationSessionLastActivityDate(sessionId);
		}

		public async Task UpdateApplicationSessionVersion(int sessionId, string? version)
		{
			await applicationSessionService.UpdateApplicationSessionVersion(sessionId, version);
		}

		public async Task<bool> HasActiveTaskSession(int sessionId)
		{
			var sessionResponse = await taskSessionService.GetActiveTaskSession(sessionId);

			if (sessionResponse == null)
				return false;

			return true;
		}

		public async Task CloseTaskSession(int sessionId)
		{
			await taskSessionService.CloseTaskSession(sessionId);
		}

		public async Task UpdateTaskSessionLastActivityDate(int sessionId)
		{
			await taskSessionService.UpdateTaskSessionLastActivityDate(sessionId);
		}

		public async Task<TerminalMandantDTO[]> GetActiveMandants()
		{
			securityContext.ThrowIfNotAuthenticated(out var userId);

			var applicationSession = await applicationSessionService.GetActiveApplicationSessionByUserId(userId);

			applicationSession.ThrowIfNotInitialized();

			var allMandants = await mandantService.GetAll();

			return allMandants
				.Select(m => new TerminalMandantDTO
				{
					Id = m.Id,
					Code = m.Code,
					Name = m.Name,
					Checked = applicationSession.ActiveMandants().Contains(m.Id)
				})
				.ToArray();
		}

		public async Task SaveActiveMandants(TerminalMandantDTO[] mandants)
		{
			securityContext.ThrowIfNotAuthenticated(out var userId);

			var applicationSession = await applicationSessionService.GetActiveApplicationSessionByUserId(userId);

			applicationSession.ThrowIfNotInitialized();

			var activeMandants = mandants
				.Where(m => m.Checked)
				.Select(m => m.Id)
				.ToArray();

			await applicationSessionService.UpdateApplicationSessionUserSettings(applicationSession!.Id,
				new ApplicationSessionUserSettings
				{
					ActiveMandants = activeMandants,
					ActiveMandantId = activeMandants.Length == 1
						? activeMandants[0]
						: null
				});

			await authenticationService.RefreshSignIn();
		}
	}
}
