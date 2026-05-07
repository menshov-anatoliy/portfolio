using Molcom.DAL.Entities.Tables.Deploy.Managers;
using Molcom.DAL.Entities.Tables.Terminals.Functionals;
using Molcom.DAL.Entities.Tables.Terminals.Mandants;
using Molcom.DAL.SqlServer.Gateways.Deploy.Models;
using Molcom.Domain.Deploy.Functionals;
using Molcom.Domain.Deploy.Managers;
using Molcom.Domain.Deploy.Managers.ValueObjects;
using Molcom.Domain.Deploy.Mandants;
using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces;

namespace Molcom.DAL.SqlServer.Factories.Deploy;

/// <summary>
/// Фабрика для создания доменных объектов Manager из сущностей БД
/// </summary>
public class ManagerFactory(IManagerActionFactory managerActionFactory,
	IManagerConditionFactory managerConditionFactory,
	IManagerVersionFactory managerVersionFactory) : IManagerFactory
{
	/// <summary>
	/// Создать объект Manager из набора сущностей БД
	/// </summary>
	/// <returns>Доменный объект Manager</returns>
	public Manager Create(IValidationHandler validationHandler,
		ManagerDb managerDb,
		ManagerAggregateDbModels managerAggregateDbModels,
		SelectTemplateAggregateDbModels selectTemplateAggregateDbModels,
		SelectObjectAggregateDbModels selectObjectAggregateDbModels)
	{
		// using var writerOther = new StopwatchDebugWriter("STOPWATCH FACTORY: ManagerFactory.Create");

		var managerId = new ManagerId(managerDb.Id);

		var mandant = GetMandant(validationHandler,
			managerDb,
			managerAggregateDbModels.AllMandantsDb);

		var mandantId = new MandantId(mandant.Id);

		var functionalInstanceId = CreateFunctionalInstanceId(validationHandler,
			managerDb.FunctionalInstanceId,
			managerAggregateDbModels.AllFunctionalInstancesDb);

		var managerVersionDb = managerAggregateDbModels.AllManagerVersionsDb.SingleOrDefault(mv => mv.Id == managerDb.VersionId);

		if (managerVersionDb == null)
			throw validationHandler.UserError($"Ошибка при сборке менеджера. Не найдено версии менеджера с идентификатором {managerDb.VersionId}");

		var managerVersion = managerVersionFactory.Create(validationHandler,
			mandantId,
			managerVersionDb,
			managerAggregateDbModels);

		var managerDefine = CreateManagerDefine(validationHandler,
			managerVersion.DefineId,
			managerAggregateDbModels.AllManagerDefinesDb);

		var conditions = managerAggregateDbModels.AllManagerSettingsDb
			.Where(ms => ms.MandantId == mandantId.Id)
			.Where(ms => ms.ManagerId == managerId.Id)
			.Where(ms => ms.Type == ManagerSettingTypesDbEnum.If)
			.Select(ms => managerConditionFactory.Create(validationHandler,
				ms,
				managerVersion.ConditionTemplateId,
				managerAggregateDbModels,
				selectTemplateAggregateDbModels,
				selectObjectAggregateDbModels))
			.ToArray();

		var actions = managerAggregateDbModels.AllManagerSettingsDb
			.Where(ms => ms.MandantId == mandantId.Id)
			.Where(ms => ms.ManagerId == managerId.Id)
			.Where(ms => ms.Type == ManagerSettingTypesDbEnum.Action)
			.Select(ms => managerActionFactory.Create(validationHandler,
				ms,
				managerVersion.ActionTemplateId,
				managerAggregateDbModels,
				selectTemplateAggregateDbModels,
				selectObjectAggregateDbModels))
			.ToArray();

		return new Manager
		{
			Id = managerId,
			MandantCode = mandant.Code,
			UniqueId = managerDb.UniqueId,
			State = MappingExtensions.Map(managerDb.State,
				value =>
				{
					if (value == ManagerStatesDbEnum.Active)
						return ManagerStatesEnum.Active;

					if (value == ManagerStatesDbEnum.ActiveCopying)
						return ManagerStatesEnum.ActiveCopying;

					if (value == ManagerStatesDbEnum.Editing)
						return ManagerStatesEnum.Editing;

					if (value == ManagerStatesDbEnum.Prepared)
						return ManagerStatesEnum.Prepared;

					if (value == ManagerStatesDbEnum.RelatePrepared)
						return ManagerStatesEnum.RelatePrepared;

					if (value == ManagerStatesDbEnum.Failed)
						return ManagerStatesEnum.Failed;

					if (value == ManagerStatesDbEnum.RelateFailed)
						return ManagerStatesEnum.RelateFailed;

					if (value == ManagerStatesDbEnum.Deploying)
						return ManagerStatesEnum.Deploying;

					if (value == ManagerStatesDbEnum.DeployError)
						return ManagerStatesEnum.DeployError;

					if (value == ManagerStatesDbEnum.RelateDeployError)
						return ManagerStatesEnum.RelateDeployError;

					if (value == ManagerStatesDbEnum.RelateDeployFatalError)
						return ManagerStatesEnum.RelateDeployFatalError;

					if (value == ManagerStatesDbEnum.DeployFatalError)
						return ManagerStatesEnum.DeployFatalError;

					if (value == ManagerStatesDbEnum.BackupActive)
						return ManagerStatesEnum.BackupActive;

					if (value == ManagerStatesDbEnum.Backup)
						return ManagerStatesEnum.Backup;

					if (value == ManagerStatesDbEnum.RelateDeploying)
						return ManagerStatesEnum.RelateDeploying;

					if (value == ManagerStatesDbEnum.RelateActive)
						return ManagerStatesEnum.RelateActive;

					throw validationHandler.NotImplementedError();
				}),
			MandantId = mandantId,
			FunctionalInstanceId = functionalInstanceId,
			Define = managerDefine,
			Version = managerVersion,
			Conditions = conditions,
			Actions = actions
		};
	}

	private MandantDb GetMandant(IValidationHandler validationHandler,
		ManagerDb managerDb,
		MandantDb[] mandantsDb)
	{
		var mandants = mandantsDb
			.Where(m => m.Id == managerDb.MandantId)
			.ToArray();

		if (mandants.Length == 0)
			throw validationHandler.ApplicationError($"Ошибка при создании модели. Не найден мандант с идентификатором {managerDb.MandantId}.");

		if (mandants.Length > 1)
			throw validationHandler.ApplicationError($"Ошибка при создании модели. Найдено более одного манданта с идентификатором {managerDb.MandantId}.");

		return mandants[0];
	}

	private FunctionalInstanceId CreateFunctionalInstanceId(IValidationHandler validationHandler,
		int functionalInstanceId,
		FunctionalInstanceDb[] functionalInstancesDb)
	{
		var functionalInstances = functionalInstancesDb
			.Where(m => m.Id == functionalInstanceId)
			.ToArray();

		if (functionalInstances.Length == 0)
			throw validationHandler.ApplicationError($"Ошибка при создании модели. Не найден экземпляр варианта функционала с идентификатором {functionalInstanceId}.");

		if (functionalInstances.Length > 1)
			throw validationHandler.ApplicationError($"Ошибка при создании модели. Найдено более одного экземпляра варианта функционала с идентификатором {functionalInstanceId}.");

		var functionalInstance = functionalInstances[0];

		return new FunctionalInstanceId(functionalInstance.Id);
	}

	private ManagerDefine CreateManagerDefine(IValidationHandler validationHandler,
		ManagerDefineId managerDefine,
		ManagerDefineDb[] managerDefinesDb)
	{
		var defines = managerDefinesDb
			.Where(m => m.Id == managerDefine.Id)
			.ToArray();

		if (defines.Length == 0)
			validationHandler.ApplicationError($"Ошибка при создании модели. Не найден мандант с идентификатором {managerDefine.Id}.");

		if (defines.Length > 1)
			validationHandler.ApplicationError($"Ошибка при создании модели. Найдено более одного манданта с идентификатором {managerDefine.Id}.");

		var defineDb = defines[0];

		return new ManagerDefine
		{
			Id = new ManagerDefineId(defineDb.Id),
			Code = defineDb.Code,
			FunctionalId = new FunctionalId(defineDb.FunctionalCode),
			IsAllowManagerMultipleCopies = defineDb.IsManyCopies
		};
	}
}

/// <summary>
/// Интерфейс фабрики для создания доменных объектов Manager из сущностей БД
/// </summary>
public interface IManagerFactory
{
	/// <summary>
	/// Создать объект Manager из набора сущностей БД
	/// </summary>
	/// <param name="validationHandler"></param>
	/// <param name="managerDb">Сущность менеджера из БД</param>
	/// <param name="managerAggregateDbModels"></param>
	/// <param name="selectTemplateAggregateDbModels"></param>
	/// <param name="selectObjectAggregateDbModels"></param>
	/// <returns>Доменный объект Manager</returns>
	Manager Create(IValidationHandler validationHandler,
		ManagerDb managerDb,
		ManagerAggregateDbModels managerAggregateDbModels,
		SelectTemplateAggregateDbModels selectTemplateAggregateDbModels,
		SelectObjectAggregateDbModels selectObjectAggregateDbModels);
}
