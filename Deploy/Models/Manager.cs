using Microsoft.Extensions.DependencyInjection;
using Molcom.Domain.Deploy.Aggregates;
using Molcom.Domain.Deploy.Fields;
using Molcom.Domain.Deploy.Functionals;
using Molcom.Domain.Deploy.Interfaces.Services;
using Molcom.Domain.Deploy.LinkedManagers;
using Molcom.Domain.Deploy.Managers.Settings;
using Molcom.Domain.Deploy.Managers.ValueObjects;
using Molcom.Domain.Deploy.Mandants;
using Molcom.Domain.Deploy.Mml.Documents.Args;
using Molcom.Domain.Deploy.SelectTemplates;
using Molcom.Domain.Deploy.SqlObjects;
using Molcom.Domain.Deploy.SqlObjects.Services;
using Molcom.Domain.Shared.Interfaces;
using Molcom.Domain.Shared.Models.Domains;
using Molcom.Domain.Shared.Validations;
using Molcom.Domain.Shared.ValuesObjects;

namespace Molcom.Domain.Deploy.Managers;

/// <summary>
/// Менеджер
/// </summary>
public class Manager : AggregateEntity<ManagerId>
{
	public required Guid UniqueId { get; init; }

	public string NormalizedUniqueId => Convert.ToString(UniqueId)!.Replace("-", "");

	/// <summary>
	/// Тип состояния менеджера
	/// </summary>
	public required ManagerStatesEnum State { get; init; }

	/// <summary>
	/// Идентификатор манданта
	/// </summary>
	public required MandantId MandantId { get; init; }

	/// <summary>
	/// Код манданта
	/// </summary>
	public required string MandantCode { get; init; }

	/// <summary>
	/// Идентификатор экземпляра функционала
	/// </summary>
	public required FunctionalInstanceId FunctionalInstanceId { get; init; }

	/// <summary>
	/// Определение менеджера
	/// </summary>
	public required ManagerDefine Define { get; init; }

	/// <summary>
	/// Версия менеджера
	/// </summary>
	public required ManagerVersion Version { get; init; }

	/// <summary>
	/// Условия
	/// TODO: Валидация - уникальные коды
	/// </summary>
	public required ManagerCondition[] Conditions { get; init; }

	/// <summary>
	/// Условия
	/// TODO: Валидация - уникальные коды
	/// </summary>
	public required ManagerAction[] Actions { get; init; }

	public override void Validate(IValidationHandler validationHandler)
	{
		base.Validate(validationHandler);

		FunctionalInstanceId.Validate(validationHandler);

		Define.Validate(validationHandler);

		Version.Validate(validationHandler);

		foreach (var condition in Conditions)
		{
			condition.Validate(validationHandler);
		}

		foreach (var action in Actions)
		{
			action.Validate(validationHandler);
		}
	}

	public async Task<ManagerVersionToSqlObjectValidateResult[]> Validate(IServiceProvider provider,
		IValidationHandler validationHandler,
		ManagerValidateArgs args)
	{
		return await Version.Validate(provider, validationHandler, this, new ManagerVersionValidateArgs
		{
			CurrentUserLogin = args.CurrentUserLogin,
			CurrentDate = args.CurrentDate,
			ExecuteMode = args.ExecuteMode,
			DeployMode = args.DeployMode,
			IsEmbeddedCheck = args.IsEmbeddedCheck,
			AllManagers = args.AllManagers,
			AllSqlObjects = args.AllSqlObjects,
			AllSqlObjectOverrides = args.AllSqlObjectOverrides,
			AllManagerVersionToSqlObjects = args.AllManagerVersionToSqlObjects,
			AllDefines = args.AllDefines,
			AllVersions = args.AllVersions,
			AllFunctionals = args.AllFunctionals,
			AllLinkedManagerToBlockAggregations = args.AllSelectTemplateCodeBlocks,
			AllLinkedManagers = args.AllLinkedManagers
		});
	}

	public async Task<ManagerVersionToSqlObjectExecuteResult[]> Execute(IServiceProvider provider,
		IValidationHandler validationHandler,
		ManagerExecuteArgs args)
	{
		return await Version.Execute(provider, validationHandler, this, new ManagerVersionExecuteArgs
		{
			CurrentUserLogin = args.CurrentUserLogin,
			CurrentDate = args.CurrentDate,
			ExecuteMode = args.ExecuteMode,
			DeployMode = args.DeployMode,
			AllMandants = args.AllMandants,
			AllManagers = args.AllManagers,
			AllSimpleManagers = args.AllSimpleManagers,
			AllManagerToSqlObjects = args.AllManagerToSqlObjects,
			AllSqlObjects = args.AllSqlObjects,
			AllSqlObjectOverrides = args.AllSqlObjectOverrides,
			AllManagerVersionToSqlObjects = args.AllManagerVersionToSqlObjects,
			AllDefines = args.AllDefines,
			AllVersions = args.AllVersions,
			AllFunctionals = args.AllFunctionals,
			AllFunctionalInstances = args.AllFunctionalInstances,
			AllManagerActions = args.AllManagerActions,
			AllEntities = args.AllEntities,
			AllFields = args.AllFields,
			AllCriteria = args.AllCriteria,
			AllProperties = args.AllProperties,
			AllCalcSources = args.AllCalcSources,
			AllPropertyValueToPropertyValues = args.AllPropertyValueToPropertyValues,
			AllCriterionValueToCriterionValues = args.AllCriterionValueToCriterionValues
		});
	}
}

public class ManagerId(int id) : IntIdentity(id)
{
}

public class ManagerLinkedManagerValidator(IValidationHandler validationHandler, Manager obj)
	: Validator(validationHandler)
{
	public override void Validate()
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Тип состояния менеджера
/// </summary>
public enum ManagerStatesEnum
{
	/// <summary>
	/// Активный
	/// </summary>
	Active,

	/// <summary>
	/// Статус на время копирования
	/// </summary>
	ActiveCopying,

	/// <summary>
	/// Редактируется
	/// </summary>
	Editing,

	/// <summary>
	/// Готовый (прошел валидацию)
	/// </summary>
	Prepared,

	/// <summary>
	/// Готовый связанный (прошел валидацию)
	/// </summary>
	RelatePrepared,

	/// <summary>
	/// Ошибочный
	/// </summary>
	Failed,

	/// <summary>
	/// Ошибочный связанный
	/// </summary>
	RelateFailed,

	/// <summary>
	/// Деплоится
	/// </summary>
	Deploying,

	/// <summary>
	/// Ошибка при разворачивании
	/// </summary>
	DeployError,

	/// <summary>
	/// Ошибка при разворачивании связанного
	/// </summary>
	RelateDeployError,

	/// <summary>
	/// Ошибка при разворачивании связанного если не удалось откатить
	/// </summary>
	RelateDeployFatalError,

	/// <summary>
	/// Ошибка при разворачивании если не удалось откатить
	/// </summary>
	DeployFatalError,

	/// <summary>
	/// Архивный, который был активным
	/// </summary>
	BackupActive,

	/// <summary>
	/// Сохраненный, который не был активным
	/// </summary>
	Backup,

	/// <summary>
	/// Деплоиться
	/// </summary>
	RelateDeploying,

	/// <summary>
	/// Активное состояние
	/// </summary>
	RelateActive
}

public class ManagerValidateArgs : ValueObject
{
	/// <summary>
	/// Дата запуска процесса
	/// </summary>
	public required DateTime CurrentDate { get; init; }

	/// <summary>
	/// Логин текущего пользователя
	/// </summary>
	public required string CurrentUserLogin { get; init; }

	/// <summary>
	/// Режим генерации документа
	/// </summary>
	public required DocumentElementExecuteModesEnum ExecuteMode { get; init; }

	/// <summary>
	/// Режим деплоя документа
	/// </summary>
	public required DocumentElementDeployModesEnum DeployMode { get; init; }

	/// <summary>
	/// Делать ли проверку контроль вложенности элемента
	/// </summary>
	public required bool IsEmbeddedCheck { get; init; }

	/// <summary>
	/// Все доступные менеджеры
	/// </summary>
	public required Manager[] AllManagers { get; init; }

	/// <summary>
	/// Все доступные объекты SQL
	/// </summary>
	public required SqlObject[] AllSqlObjects { get; init; }

	/// <summary>
	/// Все доступные переопределенные объекты SQL
	/// </summary>
	public required SqlObjectOverride[] AllSqlObjectOverrides { get; init; }

	/// <summary>
	/// Все доступные объекты SQL к версии менеджера
	/// </summary>
	public required ManagerVersionToSqlObjectSimpleAggregate[] AllManagerVersionToSqlObjects { get; init; }

	/// <summary>
	/// Все доступные определения менеджера
	/// </summary>
	public required ManagerDefine[] AllDefines { get; init; }

	/// <summary>
	/// Все доступные версии менеджера
	/// </summary>
	public required ManagerVersionSimpleAggregate[] AllVersions { get; init; }

	public required ManagerVersionSimpleAggregate[] AllSimpleVersions { get; init; }

	/// <summary>
	/// Все доступные варианты функционала
	/// </summary>
	public required Functional[] AllFunctionals { get; init; }

	public required SelectTemplateCodeBlockSimpleAggregate[] AllSelectTemplateCodeBlocks { get; init; }

	public required LinkedManager[] AllLinkedManagers { get; init; }
}

public class ManagerExecuteArgs : ValueObject
{
	/// <summary>
	/// Логин текущего пользователя
	/// </summary>
	public required string CurrentUserLogin { get; init; }

	/// <summary>
	/// Дата запуска процесса
	/// </summary>
	public required DateTime CurrentDate { get; init; }

	/// <summary>
	/// Режим генерации документа
	/// </summary>
	public required DocumentElementExecuteModesEnum ExecuteMode { get; init; }

	/// <summary>
	/// Режим деплоя документа
	/// </summary>
	public required DocumentElementDeployModesEnum DeployMode { get; init; }

	/// <summary>
	/// Все доступные манданты
	/// </summary>
	public required Mandant[] AllMandants { get; init; }

	/// <summary>
	/// Все доступные менеджеры
	/// </summary>
	public required Manager[] AllManagers { get; init; }

	/// <summary>
	/// Все доступные менеджеры
	/// </summary>
	public required ManagerSimpleAggregate[] AllSimpleManagers { get; init; }

	/// <summary>
	/// Все доступные объекты SQL к менеджеру
	/// </summary>
	public required ManagerToSqlObject[] AllManagerToSqlObjects { get; init; }

	/// <summary>
	/// Все доступные объекты SQL
	/// </summary>
	public required SqlObject[] AllSqlObjects { get; init; }

	/// <summary>
	/// Все доступные переопределенные объекты SQL
	/// </summary>
	public required SqlObjectOverride[] AllSqlObjectOverrides { get; init; }

	/// <summary>
	/// Все доступные объекты SQL к версии менеджера
	/// </summary>
	public required ManagerVersionToSqlObjectSimpleAggregate[] AllManagerVersionToSqlObjects { get; init; }

	/// <summary>
	/// Все доступные определения менеджера
	/// </summary>
	public required ManagerDefine[] AllDefines { get; init; }

	/// <summary>
	/// Все доступные версии менеджера
	/// </summary>
	public required ManagerVersionSimpleAggregate[] AllVersions { get; init; }

	/// <summary>
	/// Все доступные варианты функционала
	/// </summary>
	public required Functional[] AllFunctionals { get; init; }

	/// <summary>
	/// Все доступные экземпляры функционала
	/// </summary>
	public required FunctionalInstance[] AllFunctionalInstances { get; init; }

	/// <summary>
	/// Все доступные действия менеджера
	/// </summary>
	public required ManagerAction[] AllManagerActions { get; init; }

	/// <summary>
	/// Сущности
	/// </summary>
	public required EntityListWithMeanView[] AllEntities { get; init; }

	/// <summary>
	/// Поля
	/// </summary>
	public required Field[] AllFields { get; init; }

	/// <summary>
	/// Критерии
	/// </summary>
	public required Criterion[] AllCriteria { get; init; }

	/// <summary>
	/// Свойства
	/// </summary>
	public required Property[] AllProperties { get; init; }

	/// <summary>
	/// Описание источников зависимости варианта CALCULATE
	/// </summary>
	public required SelectTemplateCalcSource[] AllCalcSources { get; init; }

	/// <summary>
	/// Значения критериев для значения критериев
	/// </summary>
	public required PropertyValueToPropertyValue[] AllPropertyValueToPropertyValues { get; init; }

	/// <summary>
	/// Значения критериев для значения критериев
	/// </summary>
	public required CriterionValueToCriterionValue[] AllCriterionValueToCriterionValues { get; init; }
}

public class ManagerContent : ValueObject
{
	public required string[] Lines { get; init; }
}
