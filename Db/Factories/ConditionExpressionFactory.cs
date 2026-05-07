using Molcom.DAL.Entities.Extensions;
using Molcom.DAL.Entities.Tables.Deploy.SelectObjects;
using Molcom.DAL.Entities.Tables.Deploy.SelectObjects.Sorts;
using Molcom.DAL.Entities.Tables.Deploy.SelectTemplates;
using Molcom.DAL.Entities.Tables.Deploy.SelectTemplates.Enums;
using Molcom.DAL.Entities.Tables.Terminals.Navigations;
using Molcom.DAL.SqlServer.Gateways.Deploy.Models;
using Molcom.Domain.Deploy.Conditions;
using Molcom.Domain.Deploy.Managers;
using Molcom.Domain.Deploy.SelectObjects;
using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces;
using Molcom.Domain.Shared.Validations;

namespace Molcom.DAL.SqlServer.Factories.Deploy;

public class ConditionExpressionFactory : IConditionExpressionFactory
{
	private struct CodeCondition
	{
		public required int Id { get; init; }

		public required SelectTemplateLogicOperatorsDbEnum? LogicOperator { get; init; }

		public required string? Operand1 { get; init; }

		public required SelectTemplateOperatorsDbEnum? Operator { get; init; }

		public required string? Operand2 { get; init; }

		public required SelectTemplateDataTypesDbEnum DataType { get; init; }

		public required int? ParentId { get; init; }

		public required int? DependItemId { get; init; }

		public required int? ParameterId { get; init; }

		public required short Order { get; init; }
	}

	public ConditionExpression Create(IValidationHandler validationHandler,
		ManagerLineId managerLine,
		SelectObjectId selectObject,
		SelectTemplateAggregateDbModels selectTemplateAggregateDbModels,
		SelectObjectAggregateDbModels selectObjectAggregateDbModels)
	{
		//using var stopwatchDebugWriter = new StopwatchDebugWriter("STOPWATCH FACTORY: ConditionExpressionFactory.Create");

		var conditionStructs = GetConditions(selectTemplateAggregateDbModels.AllSelectTemplateCodeIfsDb,
			db => db.SelectTemplateCodeLineId == managerLine.Id,
			selectObject,
			selectTemplateAggregateDbModels.AllSelectTemplateParametersDb,
			selectObjectAggregateDbModels.AllSelectObjectParametersDb);

		if (conditionStructs.Length == 0)
			return new BitIsTrueLeafCondition();

		var expressions = HierarchyExtensions.CreateTree(conditionStructs,
			key => key.Id,
			parent => parent.ParentId,
			(parent, child) =>
			{
				if (parent is not GroupCondition group)
				{
					validationHandler.Warning($"Ошибка сборки дерева условий проверки необходимости сборки кода. Условие с идентификатором {parent.Id} не может быть родителем для условия с идентификатором {child.Id}. Условие отброшено. Проверьте настройки условий для строки кода с идентификатором {managerLine.Id}.",
						managerLine.ConvertToSql());

					return;
				}

				group.Add(child);
			},
			condition => CreateConditionElement(validationHandler,
				condition,
				selectObject,
				selectTemplateAggregateDbModels.AllSelectTemplateDependsDb,
				selectTemplateAggregateDbModels.AllSelectTemplateDependItemsDb,
				selectTemplateAggregateDbModels.AllSelectTemplateCodeIfsDb,
				selectObjectAggregateDbModels.AllSelectObjectFiltersDb,
				selectObjectAggregateDbModels.AllSelectObjectListsDb,
				selectObjectAggregateDbModels.AllSelectObjectSortItemsDb)).ToArray();

		if (expressions.Length > 1)
			throw validationHandler.UserError("Ошибка сборки дерева условий проверки необходимости сборки кода. Дерево условий в корневом уровне имеет более одного элемента.",
				managerLine.ConvertToSql());

		return expressions[0];
	}

	public ConditionExpression Create(IValidationHandler validationHandler,
		ManagerBlockId managerBlock,
		SelectObjectId selectObject,
		SelectTemplateAggregateDbModels selectTemplateAggregateDbModels,
		SelectObjectAggregateDbModels selectObjectAggregateDbModels)
	{
		var conditions = GetConditions(selectTemplateAggregateDbModels.AllSelectTemplateCodeIfsDb,
			db => db.SelectTemplateCodeBlockId == managerBlock.Id, selectObject, selectTemplateAggregateDbModels.AllSelectTemplateParametersDb,
			selectObjectAggregateDbModels.AllSelectObjectParametersDb);

		if (conditions.Length == 0)
			return new BitIsTrueLeafCondition();

		var expressions = HierarchyExtensions.CreateTree(conditions,
				key => key.Id,
				parent => parent.ParentId,
				(parent, child) =>
				{
					if (parent is not GroupCondition group)
					{
						validationHandler.Warning($"Ошибка сборки дерева условий проверки необходимости сборки кода. Условие с идентификатором {parent.Id} не может быть родителем для условия с идентификатором {child.Id}. Условие отброшено. Проверьте настройки условий для блока кода с идентификатором {managerBlock.Id}.",
							managerBlock.ConvertToSql());

						return;
					}

					group.Add(child);
				},
				current => CreateConditionElement(validationHandler,
					current,
					selectObject,
					selectTemplateAggregateDbModels.AllSelectTemplateDependsDb,
					selectTemplateAggregateDbModels.AllSelectTemplateDependItemsDb,
					selectTemplateAggregateDbModels.AllSelectTemplateCodeIfsDb,
					selectObjectAggregateDbModels.AllSelectObjectFiltersDb,
					selectObjectAggregateDbModels.AllSelectObjectListsDb,
					selectObjectAggregateDbModels.AllSelectObjectSortItemsDb))
			.ToArray();

		if (expressions.Length > 1)
			throw validationHandler.UserError(
				"Ошибка сборки дерева условий проверки необходимости сборки кода. Дерево условий в корневом уровне имеет более одного элемента.",
				managerBlock.ConvertToSql());

		return expressions[0];
	}

	private ConditionExpression CreateConditionElement(IValidationHandler validationHandler,
		CodeCondition codeCondition,
		SelectObjectId selectObject,
		SelectTemplateDependDb[] allSelectTemplateDependsDb,
		SelectTemplateDependItemDb[] allSelectTemplateDependItemsDb,
		SelectTemplateCodeIfDb[] allSelectTemplateCodeIfsDb,
		SelectObjectFilterDb[] allSelectObjectFiltersDb,
		SelectObjectListDb[] allSelectObjectListsDb,
		SelectObjectSortItemDb[] allSelectObjectSortItemsDb)
	{
		if (codeCondition.LogicOperator.HasValue)
			return new GroupCondition
			{
				Id = codeCondition.Id,
				LogicOperator = MappingExtensions.Map(codeCondition.LogicOperator!.Value, value =>
				{
					if (value == SelectTemplateLogicOperatorsDbEnum.And)
						return BooleanAlgebraLogicOperatorTypesEnum.And;

					if (value == SelectTemplateLogicOperatorsDbEnum.Or)
						return BooleanAlgebraLogicOperatorTypesEnum.Or;

					if (value == SelectTemplateLogicOperatorsDbEnum.NotAnd)
						return BooleanAlgebraLogicOperatorTypesEnum.NotAnd;

					if (value == SelectTemplateLogicOperatorsDbEnum.NotOr)
						return BooleanAlgebraLogicOperatorTypesEnum.NotOr;

					throw validationHandler.NotImplementedError();
				}),
				Order = codeCondition.Order
			};

		if (codeCondition.ParameterId.HasValue)
		{
			if (codeCondition.DataType == SelectTemplateDataTypesDbEnum.String)
			{
				return new StringLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1,
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2,
					Order = codeCondition.Order
				};
			}

			if (codeCondition.DataType == SelectTemplateDataTypesDbEnum.Int)
			{
				return new IntLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1.ToNullableInt(),
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2.ToNullableInt(),
					Order = codeCondition.Order
				};
			}

			if (codeCondition.DataType == SelectTemplateDataTypesDbEnum.BigInt)
			{
				return new BigIntLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1.ToNullableLong(),
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2.ToNullableLong(),
					Order = codeCondition.Order
				};
			}

			if (codeCondition.DataType is SelectTemplateDataTypesDbEnum.Float
			    or SelectTemplateDataTypesDbEnum.Numeric)
			{
				return new FloatLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1.ToNullableFloat(),
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2.ToNullableFloat(),
					Order = codeCondition.Order
				};
			}

			if (codeCondition.DataType == SelectTemplateDataTypesDbEnum.Bit)
			{
				return new BitLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1.ToNullableBool(),
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2.ToNullableBool(),
					Order = codeCondition.Order
				};
			}

			if (codeCondition.DataType == SelectTemplateDataTypesDbEnum.DateTime)
			{
				return new DateTimeLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1.ToNullableDateTime(),
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2.ToNullableDateTime(),
					Order = codeCondition.Order
				};
			}

			if (codeCondition.DataType == SelectTemplateDataTypesDbEnum.Guid)
			{
				return new GuidLeafCondition
				{
					Id = codeCondition.Id,
					Operand1 = codeCondition.Operand1.ToNullableGuid(),
					Operator = MapOperator(codeCondition.Operator),
					Operand2 = codeCondition.Operand2.ToNullableGuid(),
					Order = codeCondition.Order
				};
			}
		}

		if (codeCondition.DependItemId.HasValue)
		{
			return new IsZeroLeafCondition
			{
				Id = codeCondition.Id,
				Operand1 = codeCondition.Operand1.ToNullableInt(),
				Operator = MapOperator(codeCondition.Operator),
				Operand2 = GetDependItemsCount(validationHandler,
					codeCondition.Id,
					selectObject,
					allSelectTemplateDependsDb,
					allSelectTemplateDependItemsDb,
					allSelectTemplateCodeIfsDb,
					allSelectObjectFiltersDb,
					allSelectObjectListsDb,
					allSelectObjectSortItemsDb),
				Order = codeCondition.Order
			};
		}

		throw validationHandler.NotImplementedError();
	}

	public int GetDependItemsCount(IValidationHandler validationHandler,
		int selectTemplateCodeIfId,
		SelectObjectId selectObject,
		SelectTemplateDependDb[] allSelectTemplateDependsDb,
		SelectTemplateDependItemDb[] allSelectTemplateDependItemsDb,
		SelectTemplateCodeIfDb[] allSelectTemplateCodeIfsDb,
		SelectObjectFilterDb[] allSelectObjectFiltersDb,
		SelectObjectListDb[] allSelectObjectListsDb,
		SelectObjectSortItemDb[] allSelectObjectSortItemsDb)
	{
		int? count = null;

		var selectTemplateCodeIfs = allSelectTemplateCodeIfsDb
			.Where(ifs => ifs.Id == selectTemplateCodeIfId)
			.ToArray();

		var selectTemplateCodeIfDisplayName = DisplayNameAttributeExtensions.GetDisplayNameStartWithLowerLetter<SelectTemplateCodeIfDb>();

		if (selectTemplateCodeIfs.Length == 0)
			throw validationHandler.ApplicationError(
				$"Ошибка проверки блока или строки кода на условие включения. Не найдено {selectTemplateCodeIfDisplayName} с идентификатором {selectTemplateCodeIfId}.");

		if (selectTemplateCodeIfs.Length > 1)
			throw validationHandler.ApplicationError(
				$"Ошибка проверки блока или строки кода на условие включения. Найдено более одного {selectTemplateCodeIfDisplayName} с идентификатором {selectTemplateCodeIfId}.");

		var dependItemId = selectTemplateCodeIfs[0].SelectTemplateDependItemId;

		if (dependItemId == null)
			throw validationHandler.ApplicationError(
				$"Ошибка проверки блока или строки кода на условие включения. Не найден элемент зависимости шаблона (вкладка) для условия с идентификатором {selectTemplateCodeIfId}.");

		var selectTemplateDependItems = allSelectTemplateDependItemsDb
			.Where(di => di.Id == dependItemId)
			.ToArray();

		var selectTemplateDependItemDisplayName = DisplayNameAttributeExtensions.GetDisplayNameStartWithLowerLetter<SelectTemplateCodeIfDb>();

		if (selectTemplateDependItems.Length == 0)
			throw validationHandler.ApplicationError(
				$"Ошибка проверки блока или строки кода на условие включения. Не найдено {selectTemplateDependItemDisplayName} с идентификатором {dependItemId}.");

		if (selectTemplateDependItems.Length > 1)
			throw validationHandler.ApplicationError(
				$"Ошибка проверки блока или строки кода на условие включения. Найдено более одного {selectTemplateDependItemDisplayName} с идентификатором {dependItemId}.");

		var dependItem = selectTemplateDependItems[0];

		if (dependItem.Type == SelectTemplateDependItemTypesDbEnum.Condition)
		{
			count = allSelectObjectFiltersDb
				.Where(x => x.SelectObjectId == selectObject.Id)
				.Join(allSelectTemplateDependsDb,
					left => left.SelectTemplateDependId,
					right => right.Id,
					(left, right) => new { right.SelectTemplateDependItemId })
				.Count(x => x.SelectTemplateDependItemId == dependItem.Id);
		}

		if (dependItem.Type is SelectTemplateDependItemTypesDbEnum.List
		    or SelectTemplateDependItemTypesDbEnum.SpecifyList
		    or SelectTemplateDependItemTypesDbEnum.ListCondition)
		{
			count = allSelectObjectListsDb
				.Where(x => x.SelectObjectId == selectObject.Id)
				.Join(allSelectTemplateDependsDb,
					left => left.SelectTemplateDependId,
					right => right.Id,
					(left, right) => new { right.SelectTemplateDependItemId })
				.Count(x => x.SelectTemplateDependItemId == dependItem.Id);
		}

		if (dependItem.Type == SelectTemplateDependItemTypesDbEnum.Sort)
		{
			count = allSelectObjectSortItemsDb
				.Where(x => x.SelectObjectId == selectObject.Id)
				.Count(x => x.SelectTemplateDependItemId == dependItem.Id);
		}

		if (count.HasValue == false)
			throw validationHandler.NotImplementedError();

		return count.Value;
	}

	private static OperatorEnum MapOperator(SelectTemplateOperatorsDbEnum? selectTemplateOperator)
	{
		return MappingExtensions.Map(selectTemplateOperator, value =>
		{
			if (value == SelectTemplateOperatorsDbEnum.Equals)
				return OperatorEnum.Equals;

			if (value == SelectTemplateOperatorsDbEnum.NotEquals)
				return OperatorEnum.NotEquals;

			if (value == SelectTemplateOperatorsDbEnum.Contains)
				return OperatorEnum.Contains;

			if (value == SelectTemplateOperatorsDbEnum.StartsWith)
				return OperatorEnum.StartsWith;

			if (value == SelectTemplateOperatorsDbEnum.EndsWith)
				return OperatorEnum.EndsWith;

			if (value == SelectTemplateOperatorsDbEnum.NotContains)
				return OperatorEnum.NotContains;

			if (value == SelectTemplateOperatorsDbEnum.NotStartsWith)
				return OperatorEnum.NotStartsWith;

			if (value == SelectTemplateOperatorsDbEnum.NotEndsWith)
				return OperatorEnum.NotEndsWith;

			if (value == SelectTemplateOperatorsDbEnum.Less)
				return OperatorEnum.Less;

			if (value == SelectTemplateOperatorsDbEnum.LessOrEqual)
				return OperatorEnum.LessOrEqual;

			if (value == SelectTemplateOperatorsDbEnum.More)
				return OperatorEnum.More;

			if (value == SelectTemplateOperatorsDbEnum.MoreOrEqual)
				return OperatorEnum.MoreOrEqual;

			if (value == SelectTemplateOperatorsDbEnum.IsNull)
				return OperatorEnum.IsNull;

			if (value == SelectTemplateOperatorsDbEnum.NotIsNull)
				return OperatorEnum.NotIsNull;

			if (value == SelectTemplateOperatorsDbEnum.InDay)
				return OperatorEnum.InDay;

			if (value == SelectTemplateOperatorsDbEnum.NotInDay)
				return OperatorEnum.NotInDay;

			if (value == SelectTemplateOperatorsDbEnum.IsAny)
				return OperatorEnum.IsAny;

			if (value == SelectTemplateOperatorsDbEnum.IsEmpty)
				return OperatorEnum.IsEmpty;

			if (value == SelectTemplateOperatorsDbEnum.UpperContains)
				return OperatorEnum.UpperContains;

			if (value == SelectTemplateOperatorsDbEnum.UpperEndsWith)
				return OperatorEnum.UpperEndsWith;

			if (value == SelectTemplateOperatorsDbEnum.UpperStartsWith)
				return OperatorEnum.UpperStartsWith;

			if (value == SelectTemplateOperatorsDbEnum.UpperNotContains)
				return OperatorEnum.UpperNotContains;

			if (value == SelectTemplateOperatorsDbEnum.UpperNotEndsWith)
				return OperatorEnum.UpperNotEndsWith;

			if (value == SelectTemplateOperatorsDbEnum.UpperNotStartsWith)
				return OperatorEnum.UpperNotStartsWith;

			if (value == SelectTemplateOperatorsDbEnum.UpperEquals)
				return OperatorEnum.UpperEquals;

			if (value == SelectTemplateOperatorsDbEnum.UpperNotEquals)
				return OperatorEnum.UpperNotEquals;

			if (value == SelectTemplateOperatorsDbEnum.In)
				return OperatorEnum.In;

			if (value == SelectTemplateOperatorsDbEnum.NotIn)
				return OperatorEnum.NotIn;

			throw new NotImplementedException();
		});
	}

	private CodeCondition[] GetConditions(SelectTemplateCodeIfDb[] selectTemplateCodeIfsDb,
		Predicate<SelectTemplateCodeIfDb> filterPredicate,
		SelectObjectId selectObject,
		SelectTemplateParameterDb[] selectTemplateParametersDb,
		SelectObjectParameterDb[] selectObjectParametersDb)
	{
		return selectTemplateCodeIfsDb
			.Where(codeIf => filterPredicate(codeIf))
			.Select(codeIf =>
			{
				var selectTemplateParameter = selectTemplateParametersDb.SingleOrDefault(p => p.Id == codeIf.SelectTemplateParameterId);

				if (selectTemplateParameter == null)
					return new CodeCondition
					{
						Id = codeIf.Id,
						ParameterId = codeIf.SelectTemplateParameterId,
						ParentId = codeIf.SelectTemplateCodeIfParentId,
						DependItemId = codeIf.SelectTemplateDependItemId,
						LogicOperator = codeIf.LogicOperator,
						DataType = SelectTemplateDataTypesDbEnum.Int,
						Operand1 = null,
						Operator = codeIf.Operator,
						Operand2 = codeIf.Value,
						Order = codeIf.Order,
					};

				var selectObjectParameters = selectObjectParametersDb
					.Where(p => p.SelectObjectId == selectObject.Id)
					.Where(p => p.SelectTemplateParameterId == selectTemplateParameter.Id)
					.ToArray();

				if (selectObjectParameters.Length == 1)
				{
					var selectObjectParameter = selectObjectParameters[0];

					return new CodeCondition
					{
						Id = codeIf.Id,
						ParameterId = codeIf.SelectTemplateParameterId,
						ParentId = codeIf.SelectTemplateCodeIfParentId,
						DependItemId = codeIf.SelectTemplateDependItemId,
						LogicOperator = codeIf.LogicOperator,
						DataType = selectTemplateParameter.DataType,
						Operand1 = selectObjectParameter.Value,
						Operator = codeIf.Operator,
						Operand2 = codeIf.Value,
						Order = codeIf.Order,
					};
				}

				return new CodeCondition
				{
					Id = codeIf.Id,
					ParameterId = codeIf.SelectTemplateParameterId,
					ParentId = codeIf.SelectTemplateCodeIfParentId,
					DependItemId = codeIf.SelectTemplateDependItemId,
					LogicOperator = codeIf.LogicOperator,
					DataType = selectTemplateParameter.DataType,
					Operand1 = null,
					Operator = codeIf.Operator,
					Operand2 = codeIf.Value,
					Order = codeIf.Order,
				};
			})
			.ToArray();
	}
}

public interface IConditionExpressionFactory
{
	ConditionExpression Create(IValidationHandler validationHandler,
		ManagerLineId managerLine,
		SelectObjectId selectObject,
		SelectTemplateAggregateDbModels selectTemplateAggregateDbModels,
		SelectObjectAggregateDbModels selectObjectAggregateDbModels);

	ConditionExpression Create(IValidationHandler validationHandler,
		ManagerBlockId managerBlock,
		SelectObjectId selectObject,
		SelectTemplateAggregateDbModels selectTemplateAggregateDbModels,
		SelectObjectAggregateDbModels selectObjectAggregateDbModels);
}
