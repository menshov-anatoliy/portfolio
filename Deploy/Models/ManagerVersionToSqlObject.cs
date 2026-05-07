using Microsoft.Extensions.DependencyInjection;
using Molcom.Domain.Deploy.Aggregates;
using Molcom.Domain.Deploy.Fields;
using Molcom.Domain.Deploy.Functionals;
using Molcom.Domain.Deploy.Interfaces.Services;
using Molcom.Domain.Deploy.LinkedManagers;
using Molcom.Domain.Deploy.Managers;
using Molcom.Domain.Deploy.Managers.Settings;
using Molcom.Domain.Deploy.Managers.ValueObjects;
using Molcom.Domain.Deploy.Mandants;
using Molcom.Domain.Deploy.Mml.Documents;
using Molcom.Domain.Deploy.Mml.Documents.Args;
using Molcom.Domain.Deploy.SelectTemplates;
using Molcom.Domain.Deploy.SqlObjects.Services;
using Molcom.Domain.Shared.Interfaces;
using Molcom.Domain.Shared.Models.Domains;
using Molcom.Domain.Shared.Validations;
using Molcom.Domain.Shared.Validations.Notifications;

namespace Molcom.Domain.Deploy.SqlObjects;

/// <summary>
/// Объект SQL к версии менеджера
/// </summary>
public class ManagerVersionToSqlObject : SqlObject
{
	private ProcedureDocumentResult? _derivativeProcedureDocumentResult;

	public required ManagerVersionId ManagerVersionId { get; init; }

	/// <summary>
	/// Порядок создания при разворачивании манданта
	/// </summary>
	public required int DeployOrder { get; init; }

	/// <summary>
	/// Тип процедуры определения Менеджера
	/// </summary>
	public required SqlObjectTypesEnum Type { get; init; }

	/// <summary>
	/// Имя производного объекта
	/// </summary>
	public required string? DerivativeProcedureName { get; init; }

	/// <summary>
	/// Имя временного объекта для проверки создания объекта
	/// </summary>
	public required string? TemporaryCreationCheckProcedureName { get; init; }

	private async Task<ProcedureDocumentResult> GetDerivativeProcedureDocument(IServiceProvider provider,
		IValidationHandler validationHandler,
		Manager manager)
	{
		if (_derivativeProcedureDocumentResult != null)
			return _derivativeProcedureDocumentResult;

		var managerExecutionService = provider.GetRequiredService<IManagerExecutionService>();

		var mmlService = provider.GetRequiredService<IMolcomMarkupLanguageService>();

		var content = await GetSqlProcedureContent(validationHandler, managerExecutionService, manager);

		var document = mmlService.CreateDocument(validationHandler, content);

		var result = new ProcedureDocumentResult
		{
			Content = content,
			Root = document
		};

		_derivativeProcedureDocumentResult = result;

		return result;
	}

	private async Task<string> GetSqlProcedureContent(IValidationHandler validationHandler,
		IManagerExecutionService managerExecutionService,
		Manager manager)
	{
		var sqlContent = await managerExecutionService.GetSqlProcedureContent(validationHandler, manager.MandantId, Code, DerivativeProcedureName);

		if (sqlContent.IsProcedureExists == false)
			throw validationHandler.UserError(string.Format(SqlProcedureNotFoundErrorMessage, Code), this.ConvertToSql());

		return sqlContent.ProcedureContent!;

	}

	public async Task<ManagerVersionToSqlObjectValidateResult> Validate(IServiceProvider provider,
		IValidationHandler validationHandler,
		Manager manager,
		ManagerVersionToSqlObjectValidateArgs args)
	{
		string? documentContent = null;

		try
		{
			var procedureDocumentResult = await GetDerivativeProcedureDocument(provider, validationHandler, manager);

			var documentRoot = procedureDocumentResult.Root;

			documentContent = procedureDocumentResult.Content;

			documentRoot.Validate(validationHandler, manager, new DocumentElementValidateArgs
			{
				CurrentDate = args.CurrentDate,
				CurrentUserLogin = args.CurrentUserLogin,
				ExecuteMode = args.ExecuteMode,
				DeployMode = args.DeployMode,
				IsEmbeddedCheck = args.IsEmbeddedCheck,
				AllManagers = args.AllManagers,
				AllSqlObjects = args.AllSqlObjects,
				AllManagerVersionToSqlObjects = args.AllManagerVersionToSqlObjects,
				AllLinkedManagers = args.AllLinkedManagers,
				AllDefines = args.AllDefines,
				AllVersions = args.AllVersions,
				AllFunctionals = args.AllFunctionals,
				AllSqlObjectOverrides = args.AllSqlObjectOverrides,
				AllSelectTemplateCodeBlocks = args.AllLinkedManagerToBlockAggregations
			});
		}
		catch (Exception e)
		{
			return new ManagerVersionToSqlObjectValidateResult(Code,
				documentContent,
				validationHandler.Values
					.Union([new ApplicationErrorValidationNotification(e.Message)])
					.ToArray(),
				ManagerVersionToSqlObjectValidateResultStatesEnum.Failed);
		}

		return new ManagerVersionToSqlObjectValidateResult(Code,
			documentContent,
			validationHandler.Values,
			validationHandler.Values.Any(v => v.Type == ValidationNotificationTypesEnum.Error)
				? ManagerVersionToSqlObjectValidateResultStatesEnum.Failed
				: ManagerVersionToSqlObjectValidateResultStatesEnum.Success);
	}

	public async Task<ManagerVersionToSqlObjectExecuteResult> Execute(IServiceProvider provider,
		IValidationHandler validationHandler,
		Manager manager,
		ManagerVersionToSqlObjectExecuteArgs args)
	{
		string? documentContent = null;

		try
		{
			var procedureDocumentResult = await GetDerivativeProcedureDocument(provider, validationHandler, manager);

			var documentRoot = procedureDocumentResult.Root;

			documentContent = procedureDocumentResult.Content;

			await documentRoot.Execute(validationHandler, manager, new DocumentElementExecuteArgs
			{
				SqlObjectCode = Code,
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

			var mmlExecutedContent = documentRoot.ToMml(validationHandler);

			var executedProcedureContent = string.Join(mmlExecutedContent.LinesSeparator, mmlExecutedContent.Lines);

			return new ManagerVersionToSqlObjectExecuteResult(Code,
				documentContent,
				Code,
				executedProcedureContent,
				validationHandler.Values,
				validationHandler.Values.Any(v => v.Type == ValidationNotificationTypesEnum.Error)
					? ManagerVersionToSqlObjectExecuteResultStatesEnum.Failed
					: ManagerVersionToSqlObjectExecuteResultStatesEnum.Success);
		}
		catch (Exception e)
		{
			return new ManagerVersionToSqlObjectExecuteResult(Code,
				documentContent,
				Code,
				validationHandler.Values.Union([new ApplicationErrorValidationNotification(e.Message)])
					.ToArray(),
				ManagerVersionToSqlObjectExecuteResultStatesEnum.Failed);
		}
	}

}

/// <summary>
/// Тип процедуры определения Менеджера
/// </summary>
public enum SqlObjectTypesEnum
{
	/// <summary>
	/// Использование менеджера
	/// </summary>
	ManagerUse,

	/// <summary>
	/// Подготовка менеджера
	/// Должна быть процедурой без параметров, вызывается сразу после ее деплоя
	/// </summary>
	ManagerPrepare,

	/// <summary>
	/// Инициализация менеджера
	/// Должна быть процедурой без параметров, вызывается после деплоя всех объектов
	/// </summary>
	ManagerInit
}

public class ManagerVersionToSqlObjectValidateResult(string originProcedureName,
	string? originProcedureContent,
	ValidationNotification[] validation,
	ManagerVersionToSqlObjectValidateResultStatesEnum state) : ValueObject
{
	public string OriginProcedureName { get; } = originProcedureName;

	public string? OriginProcedureContent { get; } = originProcedureContent;

	public ValidationNotification[] Validation { get; } = validation;

	public ManagerVersionToSqlObjectValidateResultStatesEnum State { get; } = state;
}

public enum ManagerVersionToSqlObjectValidateResultStatesEnum
{
	Success,

	Failed
}

public class ManagerVersionToSqlObjectExecuteResult(string originProcedureName,
	string? originProcedureContent,
	string executedProcedureName,
	ValidationNotification[] validation,
	ManagerVersionToSqlObjectExecuteResultStatesEnum state) : ValueObject
{
	public ManagerVersionToSqlObjectExecuteResult(string originProcedureName,
		string originProcedureContent,
		string executedProcedureName,
		string? executedProcedureContent,
		ValidationNotification[] validation,
		ManagerVersionToSqlObjectExecuteResultStatesEnum state)
		: this(originProcedureName,
			originProcedureContent,
			executedProcedureName,
			validation,
			state)
	{
		ExecutedProcedureContent = executedProcedureContent;
	}

	public string OriginProcedureName { get; } = originProcedureName;

	public string? OriginProcedureContent { get; } = originProcedureContent;

	public string ExecutedProcedureName { get; } = executedProcedureName;

	public string? ExecutedProcedureContent { get; }

	public ValidationNotification[] Validation { get; init; } = validation;

	public ManagerVersionToSqlObjectExecuteResultStatesEnum State { get; } = state;
}

public enum ManagerVersionToSqlObjectExecuteResultStatesEnum
{
	Success,

	Failed
}

public class ManagerVersionToSqlObjectValidateArgs : ValueObject
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

	/// <summary>
	/// Все доступные варианты функционала
	/// </summary>
	public required Functional[] AllFunctionals { get; init; }

	/// <summary>
	/// Все связанные менеджеры
	/// </summary>
	public required LinkedManager[] AllLinkedManagers { get; init; }

	/// <summary>
	/// Все агрегаты связанных менеджеров с привязкой к блокам кода для проверки их на существование
	/// </summary>
	public required SelectTemplateCodeBlockSimpleAggregate[] AllLinkedManagerToBlockAggregations { get; init; }

}

public class ManagerVersionToSqlObjectExecuteArgs : ValueObject
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
	/// Все доступные варианты экземпляра функционала
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

public class ProcedureDocumentResult : ValueObject
{
	public required string Content { get; init; }

	public required RootDocumentElement Root { get; init; }
}
