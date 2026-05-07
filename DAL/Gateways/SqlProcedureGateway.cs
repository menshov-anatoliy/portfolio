using Molcom.DAL.Entities.Tables.Terminals.DbConnections.Enums;
using Molcom.DAL.SqlServer.Extensions;
using Molcom.DAL.SqlServer.Hibernate.Sessions;
using Molcom.DAL.SqlServer.Repositories;
using Molcom.Domain.Deploy.Mandants;
using Molcom.Domain.Shared.Interfaces;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Services.Deploy.Interfaces.Db;
using NHibernate;

namespace Molcom.DAL.SqlServer.Gateways.Deploy;

public class SqlProcedureGateway(ISessionFactoryProvider sessionFactoryProvider,
	IDbConnectionRepository dbConnectionRepository,
	IDbConnectionTypeRepository connectionTypeRepository) : ISqlProcedureGateway
{
	public async Task<SqlProcedureContentModel> GetSqlProcedureContent(IDbSession session,
		IValidationHandler validationHandler,
		MandantId mandantId,
		string procedureName)
	{
		var internalSession = (ISession)session.InternalSession;

		var dbConnectionType = connectionTypeRepository.GetDbConnectionType(validationHandler, internalSession, DbConnectionTypesDbEnum.Data);

		var dbConnection = dbConnectionRepository.GetDbConnection(internalSession, mandantId, dbConnectionType);

		using var overrideSession = sessionFactoryProvider.OpenSession(dbConnection.Server, dbConnection.DataBase);

		overrideSession.FlushMode = FlushMode.Manual;

		if (overrideSession.IsProcedureExists(validationHandler, procedureName) == false)
		{
			validationHandler.ApplicationError($"Ошибка при получении информации об SQL процедуре. Процедура с именем \"{procedureName}\" в базе данных не найдена.");

			return new SqlProcedureContentModel
			{
				IsExists = false,
				ProcedureContent = null
			};
		}

		string? content = null;

		try
		{
			content = await overrideSession.GetProcedureContent(validationHandler, procedureName);
		}
		catch (Exception e)
		{
			validationHandler.ApplicationError($"Ошибка при получении информации об SQL процедуре. {e.Message}\".");

			return new SqlProcedureContentModel
			{
				IsExists = false,
				ProcedureContent = null
			};
		}

		return new SqlProcedureContentModel
		{
			IsExists = true,
			ProcedureContent = content
		};
	}
}

