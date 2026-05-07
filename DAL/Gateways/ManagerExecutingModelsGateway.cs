using Molcom.DAL.Entities.Tables.Deploy.LinkedManagers;
using Molcom.DAL.Entities.Tables.Deploy.Managers;
using Molcom.DAL.Entities.Tables.Deploy.SelectObjects;
using Molcom.DAL.Entities.Tables.Deploy.SelectObjects.Sorts;
using Molcom.DAL.Entities.Tables.Deploy.SelectTemplates;
using Molcom.DAL.Entities.Tables.Deploy.SqlObjects;
using Molcom.DAL.Entities.Tables.Shared;
using Molcom.DAL.Entities.Tables.Terminals.Enums;
using Molcom.DAL.Entities.Tables.Terminals.Functionals;
using Molcom.DAL.Entities.Tables.Terminals.Mandants;
using Molcom.DAL.SqlServer.Extensions;
using Molcom.DAL.SqlServer.Factories.Deploy;
using Molcom.DAL.SqlServer.Gateways.Deploy.Models;
using Molcom.Domain.Deploy.Aggregates;
using Molcom.Domain.Deploy.Fields;
using Molcom.Domain.Deploy.Functionals;
using Molcom.Domain.Deploy.Managers;
using Molcom.Domain.Deploy.Managers.ValueObjects;
using Molcom.Domain.Deploy.Mandants;
using Molcom.Domain.Deploy.Mml.Documents.Enums;
using Molcom.Domain.Deploy.SelectTemplates;
using Molcom.Domain.Deploy.SqlObjects;
using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Services.Core.Extensions;
using Molcom.Services.Core.Infrastructures;
using Molcom.Services.Deploy.Interfaces.Db;
using Molcom.Services.Deploy.Models.Db;
using NHibernate;
using NHibernate.Linq;

namespace Molcom.DAL.SqlServer.Gateways.Deploy;

public class ManagerExecutingModelsGateway(IMandantFactory mandantFactory,
	IManagerFactory managerFactory,
	IManagerActionFactory managerActionFactory,
	ILinkedManagerFactory linkedManagerFactory) : IManagerExecutingModelsGateway
{
	private ManagerAggregateDbModels _managerAggregateDbModels;

	private SelectTemplateAggregateDbModels _selectTemplateAggregateDbModels;

	private SelectObjectAggregateDbModels _selectObjectAggregateDbModels;

	/// <summary>
	/// Загрузка сущностей из БД
	/// </summary>
	private void InitializeDbModels(ISession session, ManagerSearchingModel searchModel)
	{
		var mandantIds = searchModel.AllMandants
			.Select(m => m.Id)
			.ToArray();

		var allManagerIds = searchModel.AllMainExecuteManagers
			.Union(searchModel.AllChildManagers)
			.Union(searchModel.AllDependentExecuteManagers)
			.Select(m => m.Id)
			.ToArray();

		var mandantsDb = session.Query<MandantDb>()
			.Where(m => mandantIds.Contains(m.Id))
			.ToFuture();

		var allManagersDb = session.Query<ManagerDb>()
			.ToFuture();

		var managerDefinesDb = session.Query<ManagerDefineDb>()
			.ToFuture();

		var managerVersionsDb = session.Query<ManagerVersionDb>()
			.Where(v => v.Deleted == false)
			.ToFuture();

		var managerSettingsDb = session.Query<ManagerSettingDb>()
			.ToFuture();

		var managerVersionToSqlObjectsDb = session.Query<ManagerVersionToSqlObjectDb>()
			.ToFuture();

		var managerConditionToActionsDb = session.Query<ManagerConditionToActionDb>()
			.ToFuture();

		var functionalInstancesDb = session.Query<FunctionalInstanceDb>()
			.ToFuture();

		var functionalsDb = session.Query<FunctionalDb>()
			.ToFuture();

		var linkedManagersDb = session.Query<LinkedManagerDb>()
			.ToFuture();

		var linkedManagerToSqlObjectsDb = session.Query<LinkedManagerToSqlObjectDb>()
			.ToFuture();

		var sqlObjectsDb = session.Query<SqlObjectDb>()
			.ToFuture();

		var sqlObjectOverridesDb = session.Query<SqlObjectOverrideDb>()
			.ToFuture();

		var managerToSqlObjectsDb = session.Query<ManagerToSqlObjectDb>()
			.ToFuture();

		var entitiesDb = session.Query<EntityListWithMeanViewDb>()
			.ToFuture();

		var fieldsDb = session.Query<FieldDb>()
			.ToFuture();

		var criteriaDb = session.Query<CriterionDb>()
			.ToFuture();

		var propertiesDb = session.Query<PropertyDb>()
			.ToFuture();

		var propertyValueToPropertyValuesDb = session.Query<PropertyValueToPropertyValueDb>()
			.ToFuture();

		var criterionValueToCriterionValuesDb = session.Query<CriterionValueToCriterionValueDb>()
			.ToFuture();

		_managerAggregateDbModels = new ManagerAggregateDbModels
		{
			AllMandantsDb = mandantsDb.ToArray(),
			AllManagersDb = allManagersDb.ToArray(),
			AllManagerDefinesDb = managerDefinesDb.ToArray(),
			AllManagerVersionsDb = managerVersionsDb.ToArray(),
			AllManagerSettingsDb = managerSettingsDb.ToArray(),
			AllFunctionalInstancesDb = functionalInstancesDb.ToArray(),
			AllFunctionalDb = functionalsDb.ToArray(),
			AllManagerVersionToSqlObjectsDb = managerVersionToSqlObjectsDb.ToArray(),
			AllSqlObjectsDb = sqlObjectsDb.ToArray(),
			AllLinkedManagersDb = linkedManagersDb.ToArray(),
			AllLinkedManagerToSqlObjectsDb = linkedManagerToSqlObjectsDb.ToArray(),
			AllSqlObjectOverridesDb = sqlObjectOverridesDb.ToArray(),
			AllManagerConditionToActionsDb = managerConditionToActionsDb.ToArray(),
			AllManagerToSqlObjectsDb = managerToSqlObjectsDb.ToArray(),
			AllEntitiesDb = entitiesDb.ToArray(),
			AllFieldsDb = fieldsDb.ToArray(),
			AllCriteriaDb = criteriaDb.ToArray(),
			AllPropertiesDb = propertiesDb.ToArray(),
			AllPropertyValueToPropertyValuesDb = propertyValueToPropertyValuesDb.ToArray(),
			AllCriterionValueToCriterionValuesDb = criterionValueToCriterionValuesDb.ToArray()
		};

		var selectTemplatesDb = session.Query<SelectTemplateDb>()
			.Where(st => st.Type == TemplateTypesDbEnum.If || st.Type == TemplateTypesDbEnum.Action)
			.ToFuture();

		var selectTemplateCodeBlocksDb = session.Query<SelectTemplateCodeBlockDb>()
			.ToFuture();

		var selectTemplateCodeLinesDb = session.Query<SelectTemplateCodeLineDb>()
			.ToFuture();

		var selectTemplateParametersDb = session.Query<SelectTemplateParameterDb>()
			.ToFuture();

		var selectTemplateDependItemsDb = session.Query<SelectTemplateDependItemDb>()
			.ToFuture();

		var selectTemplateCalcSourcesDb = session.Query<SelectTemplateCalcSourceDb>()
			.ToFuture();

		var selectTemplateFunctionsDb = session.Query<SelectTemplateFunctionDb>()
			.ToFuture();

		var selectTemplateDependsDb = session.Query<SelectTemplateDependDb>()
			.ToFuture();

		var selectTemplateDependParametersDb = session.Query<SelectTemplateDependParameterDb>()
			.ToFuture();

		var selectTemplateCalcFieldsDb = session.Query<SelectTemplateCalcFieldDb>()
			.Where(cf => cf.IsDeleted == false)
			.ToFuture();

		var selectTemplateCodeСorrelationsDb = session.Query<SelectTemplateCodeСorrelationDb>()
			.ToFuture();

		var selectTemplateCodeBlockToDependsDb = session.Query<SelectTemplateCodeBlockToDependDb>()
			.ToFuture();

		var selectTemplateCodeIfsDb = session.Query<SelectTemplateCodeIfDb>()
			.ToFuture();

		var selectTemplateCodeLineParametersDb = session.Query<SelectTemplateCodeLineParameterDb>()
			.ToFuture();

		var selectTemplateCodeLineAliasesDb = session.Query<SelectTemplateCodeLineAliasDb>()
			.ToFuture();

		var fieldMatchDetailsDb = session.Query<FieldMatchDetailDb>()
			.ToFuture();

		var selectTemplateCodeLineCalculateFieldReplacesDb = session.Query<SelectTemplateCodeLineCalculateFieldReplaceDb>()
			.ToFuture();

		var selectTemplateCodeLineExceptionsDb = session.Query<SelectTemplateCodeLineExceptionDb>()
			.ToFuture();

		_selectTemplateAggregateDbModels = new SelectTemplateAggregateDbModels
		{
			AllSelectTemplatesDb = selectTemplatesDb.ToArray(),
			AllSelectTemplateParametersDb = selectTemplateParametersDb.ToArray(),
			AllSelectTemplateDependItemsDb = selectTemplateDependItemsDb.ToArray(),
			AllSelectTemplateCalcSourcesDb = selectTemplateCalcSourcesDb.ToArray(),
			AllSelectTemplateFunctionsDb = selectTemplateFunctionsDb.ToArray(),
			AllSelectTemplateDependsDb = selectTemplateDependsDb.ToArray(),
			AllSelectTemplateDependParametersDb = selectTemplateDependParametersDb.ToArray(),
			AllSelectTemplateCalcFieldsDb = selectTemplateCalcFieldsDb.ToArray(),
			AllSelectTemplateCodeBlocksDb = selectTemplateCodeBlocksDb.ToArray(),
			AllSelectTemplateCodeLinesDb = selectTemplateCodeLinesDb.ToArray(),
			AllSelectTemplateCodeСorrelationsDb = selectTemplateCodeСorrelationsDb.ToArray(),
			AllSelectTemplateCodeBlockToDependsDb = selectTemplateCodeBlockToDependsDb.ToArray(),
			AllSelectTemplateCodeIfsDb = selectTemplateCodeIfsDb.ToArray(),
			AllSelectTemplateCodeLineParametersDb = selectTemplateCodeLineParametersDb.ToArray(),
			AllSelectTemplateCodeLineAliasesDb = selectTemplateCodeLineAliasesDb.ToArray(),
			AllFieldMatchDetailsDb = fieldMatchDetailsDb.ToArray(),
			AllSelectTemplateCodeLineCalculateFieldReplacesDb = selectTemplateCodeLineCalculateFieldReplacesDb.ToArray(),
			AllSelectTemplateCodeLineExceptionsDb = selectTemplateCodeLineExceptionsDb.ToArray()
		};

		var selectObjectParametersDb = session.Query<SelectObjectParameterDb>()
			.ToFuture();

		var selectObjectFiltersDb = session.Query<SelectObjectFilterDb>()
			.ToFuture();

		var selectObjectListsDb = session.Query<SelectObjectListDb>()
			.ToFuture();

		var selectObjectSortFiltersDb = session.Query<SelectObjectSortFilterDb>()
			.ToFuture();

		var selectObjectSortItemsDb = session.Query<SelectObjectSortItemDb>()
			.ToFuture();

		var selectObjectSortTargetsDb = session.Query<SelectObjectSortTargetDb>()
			.ToFuture();

		_selectObjectAggregateDbModels = new SelectObjectAggregateDbModels
		{
			AllSelectObjectParametersDb = selectObjectParametersDb.ToArray(),
			AllSelectObjectFiltersDb = selectObjectFiltersDb.ToArray(),
			AllSelectObjectListsDb = selectObjectListsDb.ToArray(),
			AllSelectObjectSortFiltersDb = selectObjectSortFiltersDb.ToArray(),
			AllSelectObjectSortItemsDb = selectObjectSortItemsDb.ToArray(),
			AllSelectObjectSortTargetsDb = selectObjectSortTargetsDb.ToArray()
		};
	}

	public ManagerExecutingModels Get(IValidationHandler validationHandler, IDbSession session, ManagerSearchingModel searchModel)
	{
		var internalSession = (ISession)session.InternalSession;

		var writerInitializeDbModels = new StopwatchDebugWriter("STOPWATCH: ManagerPrepareModelsGateway.Get.InitializeDbModels");

		InitializeDbModels(internalSession, searchModel);

		writerInitializeDbModels.Dispose();

		using var writerOther = new StopwatchDebugWriter("STOPWATCH: ManagerPrepareModelsGateway.Get Factory Assemblies");

		var allMandants = _managerAggregateDbModels.AllMandantsDb
			.Select(m => mandantFactory.Create(validationHandler,
				m))
			.ToArray();

		var allManagers = _managerAggregateDbModels.AllManagersDb
			.Select(m => new ManagerSimpleAggregate
			{
				Id = new ManagerId(m.Id),
				UniqueId = m.UniqueId,
				State = MappingExtensions.Map(m.State, value =>
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
				MandantId = new MandantId(m.MandantId),
				FunctionalInstanceId = new FunctionalInstanceId(m.FunctionalInstanceId),
				ManagerVersionId = new ManagerVersionId(m.VersionId)
			})
			.ToArray();

		var allExecuteManagersIdentities = searchModel.AllMainExecuteManagers
			.Union(searchModel.AllChildManagers)
			.Union(searchModel.AllDependentExecuteManagers)
			.ToArray();

		var allExecuteManagers = _managerAggregateDbModels.AllManagersDb
			.Join(allExecuteManagersIdentities,
				left => left.Id,
				right => right.Id,
				(left, right) => left)
			.ForAllAsync(db => managerFactory.Create(validationHandler,
				db,
				_managerAggregateDbModels,
				_selectTemplateAggregateDbModels,
				_selectObjectAggregateDbModels))
			.ToArray();

		var allMainExecuteManagers = allExecuteManagers
			.Join(searchModel.AllMainExecuteManagers
					.Where(m =>
					{
						// Исключаем из исполняемых менеджеров те, которые определены как зависимые.
						// Зависимые менеджеры публикуются после исполняемых
						if (searchModel.AllDependentExecuteManagers.Any(d => d.Id == m.Id))
							return false;

						return true;
					}),
				left => left.Id.Id,
				right => right.Id,
				(left, right) => left)

			.ToArray();

		var allChildManagers = allExecuteManagers
			.Join(searchModel.AllChildManagers,
				left => left.Id.Id,
				right => right.Id,
				(left, right) => left)
			.ToArray();

		var allDependentExecuteManagers = allExecuteManagers
			.Join(searchModel.AllDependentExecuteManagers,
				left => left.Id.Id,
				right => right.Id,
				(left, right) => left)
			.ToArray();

		var allManagerActions = _managerAggregateDbModels.AllManagerSettingsDb
			.Where(s => s.Type == ManagerSettingTypesDbEnum.Action)
			.Join(searchModel.AllChildManagers,
				left => left.ManagerId,
				right => right.Id,
				(left, right) => left)
			.Join(_managerAggregateDbModels.AllManagersDb,
				left => left.ManagerId,
				right => right.Id,
				(left, right) => managerActionFactory.Create(validationHandler,
					left,
					new SelectTemplateId(right.Id),
					_managerAggregateDbModels,
					_selectTemplateAggregateDbModels,
					_selectObjectAggregateDbModels))
			.ToArray();

		var managerDefines = _managerAggregateDbModels.AllManagerDefinesDb
			.Select(d => new ManagerDefine
			{
				Id = new ManagerDefineId(d.Id),
				Code = d.Code,
				FunctionalId = new FunctionalId(d.FunctionalCode),
				IsAllowManagerMultipleCopies = d.IsManyCopies
			})
			.ToArray();

		var executingModels = new ManagerExecutingModels
		{
			AllMandants = allMandants,
			AllManagers = allExecuteManagers,
			AllSimpleManagers = allManagers,
			AllMainExecuteManagers = allMainExecuteManagers,
			AllDependentExecuteManagers = allDependentExecuteManagers,
			AllChildManagers = allChildManagers,
			AllManagerActions = allManagerActions,
			AllLinkedManagers = _managerAggregateDbModels.AllLinkedManagersDb
				.Select(lm => linkedManagerFactory.Create(validationHandler, lm))
				.ToArray(),
			AllSqlObjects = _managerAggregateDbModels.AllSqlObjectsDb
				.Select(so => new SqlObject
				{
					Code = so.Code,
					CodeCommon = so.CodeCommon,
					Base = so.Base,
					DerivativeProcedureMask = so.DerivativeMask,
					TemporaryCreationCheckProcedureMask = so.AttemptMask,
				})
				.ToArray(),
			AllManagerVersionToSqlObjects = _managerAggregateDbModels.AllManagerVersionToSqlObjectsDb
				.Select(mv2so => new ManagerVersionToSqlObjectSimpleAggregate
				{
					Code = mv2so.SqlObjectCode,
					ManagerVersionId = new ManagerVersionId(mv2so.ManagerVersionId)
				})
				.ToArray(),
			AllDefines = managerDefines,
			AllVersions = _managerAggregateDbModels.AllManagerVersionsDb
				.Select(v => new ManagerVersionSimpleAggregate
				{
					Id = new ManagerVersionId(v.Id),
					ActionTemplateId = new SelectTemplateId(v.ActionTemplateId),
					ConditionTemplateId = new SelectTemplateId(v.IfTemplateId),
					ManagerDefineId = new ManagerDefineId(v.ManagerDefineId),
					ScenarioType = MappingExtensions.Map(v.Scenario, value =>
					{
						if (value == ManagerVersionScenarioSDbEnum.ActionOnly)
							return ManagerVersionScenarioTypesEnum.ActionOnly;

						if (value == ManagerVersionScenarioSDbEnum.ActionUseIf)
							return ManagerVersionScenarioTypesEnum.ActionUseIf;

						if (value == ManagerVersionScenarioSDbEnum.If2Action)
							return ManagerVersionScenarioTypesEnum.If2Action;

						if (value == ManagerVersionScenarioSDbEnum.IfHaveAction)
							return ManagerVersionScenarioTypesEnum.IfHaveAction;

						throw validationHandler.NotImplementedError();
					}),
					IsActionMultiply = v.IsActionMultiply
				})
				.ToArray(),
			AllFunctionals = _managerAggregateDbModels.AllFunctionalDb
				.Select(f => new Functional
				{
					Id = new FunctionalId(f.Code)
				})
				.ToArray(),
			AllFunctionalInstances = _managerAggregateDbModels.AllFunctionalInstancesDb
				.Select(fi => new FunctionalInstance
				{
					Id = new FunctionalInstanceId(fi.Id),
					Name = fi.Name,
					Guid = fi.Guid,
					Description = fi.Description,
					UserVariantCode = fi.UserVariantCode,
					IsDefault = fi.IsDefault
				})
				.ToArray(),
			AllManagerToSqlObjects = _managerAggregateDbModels.AllManagerToSqlObjectsDb
				.Join(_managerAggregateDbModels.AllSqlObjectsDb,
					left => left.SqlObjectCode,
					right => right.Code,
					(left,
						right) => new ManagerToSqlObject
					{
						MandantId = new MandantId(left.MandantId),
						ManagerId = new ManagerId(left.ManagerId),
						DerivativeCode = left.Derivative,
						Code = left.SqlObjectCode,
						CodeCommon = right.CodeCommon,
						Base = right.Base,
						DerivativeProcedureMask = right.DerivativeMask,
						TemporaryCreationCheckProcedureMask = right.AttemptMask
					})
				.ToArray(),
			AllSqlObjectOverrides = _managerAggregateDbModels.AllSqlObjectOverridesDb
				.Join(_managerAggregateDbModels.AllSqlObjectsDb,
					left => left.SqlObjectCode,
					right => right.Code,
					(left,
						right) => new SqlObjectOverride
					{
						MandantId = new MandantId(left.MandantId),
						SqlObject = new SqlObject
						{
							Code = right.Code,
							CodeCommon = right.CodeCommon,
							Base = right.Base,
							DerivativeProcedureMask = right.DerivativeMask,
							TemporaryCreationCheckProcedureMask = right.AttemptMask
						},
						OverriddenCode = right.Code
					})
				.ToArray(),
			AllEntities = _managerAggregateDbModels.AllEntitiesDb
				.Select(e => new EntityListWithMeanView
				{
					EntitySourceId = e.Entity,
					EntityCode = e.EntityListCode
				})
				.ToArray(),
			AllFields = _managerAggregateDbModels.AllFieldsDb
				.Select(f => new Field
				{
					Id = new FieldId(f.Id),
					Name = f.Name,
					Code = f.Code,
					Entity = f.EntityCode,
					SqlType = f.SqlType,
					IsNullable = f.IsNullable,
					DataType = MappingExtensions.Map(f.DataType,
						value =>
						{
							if (value == FieldDataTypesDbEnum.String)
								return FieldDataTypesEnum.String;

							if (value == FieldDataTypesDbEnum.Int)
								return FieldDataTypesEnum.Int;

							if (value == FieldDataTypesDbEnum.Float)
								return FieldDataTypesEnum.Float;

							if (value == FieldDataTypesDbEnum.Datetime)
								return FieldDataTypesEnum.Datetime;

							if (value == FieldDataTypesDbEnum.Bit)
								return FieldDataTypesEnum.Bit;

							if (value == FieldDataTypesDbEnum.Date)
								return FieldDataTypesEnum.Date;

							if (value == FieldDataTypesDbEnum.Guid)
								return FieldDataTypesEnum.Guid;

							if (value == FieldDataTypesDbEnum.BigInt)
								return FieldDataTypesEnum.BigInt;

							if (value == FieldDataTypesDbEnum.Numeric)
								return FieldDataTypesEnum.Numeric;

							if (value == FieldDataTypesDbEnum.Barcode)
								return FieldDataTypesEnum.Barcode;

							if (value == FieldDataTypesDbEnum.Files)
								return FieldDataTypesEnum.Files;

							throw validationHandler.NotImplementedError();
						})
				})
				.ToArray(),
			AllCriteria = _managerAggregateDbModels.AllCriteriaDb
				.Select(c => new Criterion
				{
					Id = new CriterionId(c.Id),
					Name = c.Name,
					FieldCode = c.FieldCode
				})
				.ToArray(),
			AllProperties = _managerAggregateDbModels.AllPropertiesDb
				.Select(p => new Property
				{
					Id = new PropertyId(p.Id),
					Name = p.Name,
					FieldCode = p.FieldCode
				})
				.ToArray(),
			AllCalcSources = _selectTemplateAggregateDbModels.AllSelectTemplateCalcSourcesDb
				.Select(cs => new SelectTemplateCalcSource
				{
					Id = new SelectTemplateCalcSourceId(cs.Id),
					SelectTemplateId = new SelectTemplateId(cs.SelectTemplateId),
					Code = cs.Code,
					CalcFields = _selectTemplateAggregateDbModels.AllSelectTemplateCalcFieldsDb
						.Where(cf => cf.SelectTemplateCalcSourceId == cs.Id)
						.Select(cf => new SelectTemplateCalcField
						{
							Id = new SelectTemplateCalcFieldId(cf.Id),
							SelectTemplateCalcSourceId = new SelectTemplateCalcSourceId(cs.Id),
							Code = cf.Code,
							Name = cf.Name,
							DataType = MappingExtensions.Map(cf.DataType,
								value =>
								{
									if (value == FieldDataTypesDbEnum.String)
										return FieldDataTypesEnum.String;

									if (value == FieldDataTypesDbEnum.Int)
										return FieldDataTypesEnum.Int;

									if (value == FieldDataTypesDbEnum.Float)
										return FieldDataTypesEnum.Float;

									if (value == FieldDataTypesDbEnum.Datetime)
										return FieldDataTypesEnum.Datetime;

									if (value == FieldDataTypesDbEnum.Bit)
										return FieldDataTypesEnum.Bit;

									if (value == FieldDataTypesDbEnum.Date)
										return FieldDataTypesEnum.Date;

									if (value == FieldDataTypesDbEnum.Guid)
										return FieldDataTypesEnum.Guid;

									if (value == FieldDataTypesDbEnum.BigInt)
										return FieldDataTypesEnum.BigInt;

									if (value == FieldDataTypesDbEnum.Numeric)
										return FieldDataTypesEnum.Numeric;

									if (value == FieldDataTypesDbEnum.Barcode)
										return FieldDataTypesEnum.Barcode;

									if (value == FieldDataTypesDbEnum.Files)
										return FieldDataTypesEnum.Files;

									throw validationHandler.NotImplementedError();
								}),
							SqlType = cf.SqlType,
						})
						.ToArray()
				})
				.ToArray(),
			AllPropertyValueToPropertyValues = _managerAggregateDbModels.AllPropertyValueToPropertyValuesDb
				.Select(pv2pv => new PropertyValueToPropertyValue
				{
					Id = new PropertyValueToPropertyValueId(pv2pv.Id)
				})
				.ToArray(),
			AllCriterionValueToCriterionValues = _managerAggregateDbModels.AllCriterionValueToCriterionValuesDb
				.Select(cv2cv => new CriterionValueToCriterionValue
				{
					Id = new CriterionValueToCriterionValueId(cv2cv.Id)
				})
				.ToArray(),
			AllSelectTemplateCodeBlocks = _selectTemplateAggregateDbModels.AllSelectTemplateCodeBlocksDb
				.Select(b => new SelectTemplateCodeBlockSimpleAggregate
				{
					SelectTemplateId = b.SelectTemplateId,
					Name = b.Name,
					Code = b.Code,
					Order = b.Order
				})
				.ToArray()
		};

		return executingModels;
	}
}
