using Microsoft.Extensions.Options;
using Molcom.DAL.Entities.Tables.Terminals.Navigations;
using Molcom.DAL.Entities.Tables.Terminals.Sessions;
using Molcom.DAL.Entities.Tables.Terminals.Sessions.Interfaces;
using Molcom.DAL.SqlServer.Repositories.Sessions;
using Molcom.DAL.SqlServer.Repositories.Terminals;
using Molcom.Domain.Terminal.Models.Sessions;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Core.Interfaces.Cache;
using Molcom.Services.Core.Interfaces.Cache.LifeTimes;
using Molcom.Services.Core.Interfaces.Security;
using Molcom.Services.Terminal.Interfaces.Db.Sessions;
using NHibernate;
using NHibernate.Linq;

namespace Molcom.DAL.SqlServer.Gateways.Sessions;

public class TaskSessionGateway(IOptions<MocksOptions> mockOptions,
	IOptions<SessionTimeToLiveOptions> sessionTimeToLiveOptions,
	IApplicationSessionRepository applicationSessionRepository,
	ITaskSessionRepository taskSessionRepository,
	ITerminalTaskRepository terminalTaskRepository,
	IMemoryCacheManager memoryCacheManager,
	ISecurityContext securityContext)
	: BaseGateway(mockOptions), ITaskSessionDbGateway
{
	private readonly SessionTimeToLiveOptions _sessionTimeToLiveOptions = sessionTimeToLiveOptions.Value;

	public async Task<TaskSession> OpenTaskSession(ISession session, int sessionId, string task, int[] activeMandants)
	{
		await CloseTaskSessions(session, sessionId, task);

		var currentSession = await applicationSessionRepository.GetAsync(session, sessionId);

		var currentTask = await GetTask(session, task);

		var currentTaskId = currentTask.Id;

		var terminalActiveMandants = session.Query<TerminalTaskToMandantDb>()
			.Where(t2m => activeMandants.Contains(t2m.Mandant.Id))
			.Where(t2m => t2m.Task.Id == currentTaskId)
			.Select(t2m => t2m.Mandant)
			.ToArray();

		var newTerminalSession = new TaskSessionDb
		{
			ApplicationSession = currentSession,
			Task = currentTask,
			BeginDate = DateTime.Now,
			TimeToLiveInMinutes = _sessionTimeToLiveOptions.TaskInMinutes
		};

		newTerminalSession.Mandants = terminalActiveMandants
			.Select(m => new TaskSessionToMandantDb
			{
				Mandant = m,
				TaskSession = newTerminalSession
			}).ToList();

		var taskSessionId = (int)await taskSessionRepository.SaveAsync(session, newTerminalSession);

		return new TaskSession
		{
			Id = taskSessionId
		};
	}

	public async Task CloseTaskSessions(ISession session, int applicationSessionId, string task)
	{
		var currentTask = await GetTask(session, task);

		var currentTaskId = currentTask.Id;

		var terminalSessionsToClose = (await session.Query<TaskSessionDb>()
				.Where(s => s.ApplicationSession.Id == applicationSessionId)
				.Where(s => s.Task.Id == currentTaskId)
				.Where(s => s.EndDate.HasValue == false)
				.ToListAsync())
			.ToArray();

		foreach (var terminalSessionToClose in terminalSessionsToClose)
		{
			await CloseTaskSession(session, terminalSessionToClose);
		}
	}

	private async Task CloseTaskSession(ISession session, TaskSessionDb sessionToClose)
	{
		if (sessionToClose.EndDate.HasValue)
			return;

		sessionToClose.Close();

		await session.UpdateAsync(sessionToClose);

		await session.DeleteAsync(sessionToClose);

		securityContext.ThrowIfNotAuthenticated(out var userId);

		memoryCacheManager.RemoveAllServicesCache<ICachedPerTaskGateway>(userId);
	}

	public async Task<TaskSession?> GetActiveTaskSession(ISession session, int applicationSessionId, string task)
	{
		var activeSession = await GetActiveTaskSessionDb(session, applicationSessionId, task);

		if (activeSession == null)
			return default;

		return new TaskSession
		{
			Id = activeSession.Id
		};
	}

	private async Task<TaskSessionDb?> GetActiveTaskSessionDb(ISession session, int applicationSessionId, string task)
	{
		var currentTask = await GetTask(session, task);

		var currentTaskId = currentTask.Id;

		var activeTaskSessions = (await session.Query<TaskSessionDb>()
				.Where(s => s.ApplicationSession.Id == applicationSessionId)
				.Where(s => s.Task.Id == currentTaskId)
				.Where(s => s.EndDate.HasValue == false)
				.ToListAsync())
			.Where(s => s.IsExpired() == false)
			.ToArray();

		if (activeTaskSessions.Length == 0)
			return default;

		if (activeTaskSessions.Length > 1)
			throw new ApplicationException("Найдено несколько активных сессий терминальной задачи. Обратитесь к разработчикам.");

		var activeTaskSession = activeTaskSessions[0];

		return activeTaskSession;
	}

	private async Task<TerminalTaskDb> GetTask(ISession session, string task)
	{
		return await terminalTaskRepository.GetSingleAsync(session,
			t => string.Equals(t.Define.Code, task, StringComparison.CurrentCultureIgnoreCase));
	}

	public async Task<TaskSession?> GetActiveTaskSession(ISession session, int sessionId)
	{
		var activeSession = await GetActiveTaskSessionDb(session, sessionId);

		if (activeSession == null)
			return default;

		return new TaskSession
		{
			Id = activeSession.Id
		};
	}

	private async Task<TaskSessionDb?> GetActiveTaskSessionDb(ISession session, int sessionId)
	{
		var activeTaskSessions = (await session.Query<TaskSessionDb>()
				.Where(s => s.Id == sessionId)
				.Where(s => s.EndDate.HasValue == false)
				.ToListAsync())
			.Where(s => s.IsExpired() == false)
			.ToArray();

		if (activeTaskSessions.Length == 0)
			return default;

		if (activeTaskSessions.Length > 1)
			throw new ApplicationException("Найдено несколько активных сессий терминальной задачи. Обратитесь к разработчикам.");

		var activeTaskSession = activeTaskSessions[0];

		return activeTaskSession;
	}

	public async Task UpdateTaskSessionMandant(ISession session, int sessionId, int? mandantId)
	{
		var activeTaskSession = await GetActiveTaskSessionDb(session, sessionId);

		activeTaskSession.ThrowIfNotInitialized();

		activeTaskSession!.MandantId = mandantId;

		await taskSessionRepository.UpdateAsync(session, activeTaskSession);
	}

	public async Task UpdateTaskSessionLastActivityDate(ISession session, int sessionId)
	{
		var activeTaskSession = await GetActiveTaskSessionDb(session, sessionId);

		activeTaskSession.ThrowIfNotInitialized();

		activeTaskSession!.InvokeActivity();

		await taskSessionRepository.UpdateAsync(session, activeTaskSession);
	}

	public async Task CloseTaskSession(ISession session, int sessionId)
	{
		var sessionToClose = await taskSessionRepository.GetOrDefaultAsync(session, sessionId);

		if (sessionToClose == null)
			return;

		await CloseTaskSession(session, sessionToClose);
	}
}
