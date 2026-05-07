using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Molcom.Domain.Shared.Interfaces;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Domain.Terminal.Extensions;
using Molcom.Domain.Terminal.Interfaces.Services;
using Molcom.Domain.Terminal.Models.Enums;
using Molcom.Domain.Terminal.Models.Events;
using Molcom.Domain.Terminal.Models.Fields;
using Molcom.Domain.Terminal.Models.Reactions.Responses;
using Molcom.Domain.Terminal.Models.StateTransition.Requests;
using Molcom.Domain.Terminal.Models.StateTransition.Responses;
using Molcom.Domain.Terminal.Models.StateTransition.Responses.Final;
using Molcom.Domain.Terminal.Models.StateTransition.Responses.Intermediate;
using Molcom.Domain.Terminal.Models.Values;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Terminal.Extensions.Forms;
using Molcom.Services.Terminal.Interfaces.Db.Navigations;
using Molcom.Services.Terminal.Interfaces.Db.Reactions;
using Molcom.Services.Terminal.Managers.Events;
using Molcom.Services.Terminal.Managers.Holders;
using Molcom.Services.Terminal.Managers.StateTransition;

namespace Molcom.Services.Terminal.Services
{
	public class NavigationTerminalService(
		IOptions<MocksOptions> mockOptions,
		IServiceProvider provider,
		ITerminalNavigationCachedGateway navigations,
		ITerminalReactionCachedGateway reactions,
		IStateTransitionManager transitionManager,
		ITransitionFlowHolderManager transitionFlowHolderManager,
		IEventHistoryManager eventHistoryManager,
		IPostedValuesHolderManager postedValuesHolderManager,
		IAllValuesHolderManager allValuesHolderManager,
		IAllPropertiesHolderManager defaultPropertiesHolderManager,
		IValidationHandler validationHandler,
		ISimpleDialogHolderManager simpleDialogHolderManager)
		: TerminalService(mockOptions), INavigationTerminalService
	{
		public async Task<FinalStateTransitionResponse> GetStartState(string task)
		{
			await using var scope = provider.CreateAsyncScope();

			var serviceProvider = scope.ServiceProvider;

			using var readOnlySession = serviceProvider.GetRequiredService<IDbSession>();

			readOnlySession.Open(DbSessionReadWriteModesEnum.ReadOnly);

			var allForms = await navigations.GetAll(readOnlySession, task);

			var allDefaultProperties = allForms
				.SelectMany(TerminalPropertyExtensions.Map)
				.ToArray();

			defaultPropertiesHolderManager.Append(allDefaultProperties);

			allForms.ApplyDefaultValues(postedValuesHolderManager);

			var allValues = allForms
				.SelectMany(TerminalValueExtensions.Map)
				.ToArray();

			allValuesHolderManager.Append(allValues);

			var allReactions = await reactions.GetAll(readOnlySession, task);

			StateTransitionRequest request = new StartStateTransitionRequest(allForms,
				TerminalEventsEnum.TaskStart,
				default,
				default,
				task,
				TerminalEventsEnum.TaskStart,
				allReactions);

			transitionFlowHolderManager.Clear();

			StateTransitionResponse response;

			using var writeSession = serviceProvider.GetRequiredService<IDbSession>();

			writeSession.Open(DbSessionReadWriteModesEnum.Write);

			using var transaction = writeSession.BeginTransaction();

			do
			{
				response = await transitionManager.Next(writeSession, task, request);

				if (response is not NoActionFinalStateTransitionResponse && 
				    response is not ActionDialogFinalStateTransitionResponse)
				{
					if (response is FieldIntermediateStateTransitionResponse fieldIntermediate)
					{
						if (allForms.FindField(fieldIntermediate.Current) is ITerminalArithmeticExpressionField arithmeticField)
							arithmeticField.Evaluate();
					}

					eventHistoryManager.Append(response);

					var result = postedValuesHolderManager.Append(allForms, response);

					allForms.ApplyValues(result);
				}

				if (response is IntermediateStateTransitionResponse intermediateResponse)
				{
					request = intermediateResponse.GetRequest(allForms,
						allReactions,
						task,
						TerminalEventsEnum.TaskStart);
				}

			} while (response.IsFinal == false);

			await transaction.Commit();

			return ((FinalStateTransitionResponse)response)
				.WithTransitions(transitionFlowHolderManager.Values)
				.WithValues(postedValuesHolderManager.Values)
				.WithEvents(eventHistoryManager.Values)
				.WithValidations(validationHandler.Values)
				.WithSound()
				.WithSimpleDialogs(simpleDialogHolderManager.Values);
		}

		public async Task<FinalStateTransitionResponse> GetNextState(string task,
			TerminalValue[] allValues,
			TerminalProperty[] allProperties,
			TerminalHistoryEvent[] actions,
			TerminalValue[] postedValues,
			object? value,
			string? display,
			string focused,
			string? menu,
			TerminalEventsEnum @event)
		{
			await using var scope = provider.CreateAsyncScope();

			var serviceProvider = scope.ServiceProvider;

			using var readOnlySession = serviceProvider.GetRequiredService<IDbSession>();

			readOnlySession.Open(DbSessionReadWriteModesEnum.ReadOnly);

			// Начало любого движения фокуса сопряжено с уходом с текущего контрола, добавляем его в стек значений
			var current = @event == TerminalEventsEnum.MenuCall
				? menu!
				: focused;

			eventHistoryManager.Initialize(actions);

			var allForms = await navigations.GetAll(readOnlySession, task);

			var allDefaultProperties = allForms
				.SelectMany(TerminalPropertyExtensions.Map)
				.ToArray();

			defaultPropertiesHolderManager.Append(allDefaultProperties);

			postedValuesHolderManager.Append(allForms, postedValues);

			var currentValue = allValues.SingleWithValue(current, value, display);

			currentValue.SetSignificant();

			currentValue.SetFocused(focused);

			postedValuesHolderManager.Append(allForms, @event, currentValue);

			postedValuesHolderManager.SetInitialized();

			var allValuesWithUpdatedCurrent = allValues
				.WithValue(focused, value, display)
				.Where(v =>
				{
					// При FieldEscape удаляем из текущей коллекции элемент focused
					if (@event == TerminalEventsEnum.FieldEscape)
						return string.Equals(v.Code, focused, StringComparison.CurrentCultureIgnoreCase) == false;

					return true;

				})
				.ToArray();

			allValuesHolderManager.Append(allValuesWithUpdatedCurrent);

			allForms.ApplyProperties(allProperties)
				.ApplyValues(postedValuesHolderManager,
					allValuesWithUpdatedCurrent);

			var allReactions = await reactions.GetAll(readOnlySession, task);

			transitionFlowHolderManager.Clear();

			StateTransitionRequest request = new StartStateTransitionRequest(allForms,
				@event,
				focused,
				menu,
				focused,
				@event,
				allReactions);

			StateTransitionResponse response;

			using var writeSession = serviceProvider.GetRequiredService<IDbSession>();

			writeSession.Open(DbSessionReadWriteModesEnum.Write);

			using var transaction = writeSession.BeginTransaction();

			do
			{
				response = await transitionManager.Next(writeSession, task, request);

				if (response is not NoActionFinalStateTransitionResponse &&
				    response is not ActionDialogFinalStateTransitionResponse)
				{
					if (response is FieldIntermediateStateTransitionResponse fieldIntermediate)
					{
						if (allForms.FindField(fieldIntermediate.Current) is ITerminalArithmeticExpressionField arithmeticField)
							arithmeticField.Evaluate();
					}

					eventHistoryManager.Append(response);

					var result = postedValuesHolderManager.Append(allForms, response);

					allForms.ApplyValues(result);
				}

				if (response is IntermediateStateTransitionResponse intermediateResponse)
				{
					request = intermediateResponse.GetRequest(allForms,
						allReactions,
						focused,
						@event);
				}

			} while (response.IsFinal == false);

			await transaction.Commit();

			return ((FinalStateTransitionResponse)response)
				.WithTransitions(transitionFlowHolderManager.Values)
				.WithValues(postedValuesHolderManager.Values)
				.WithEvents(eventHistoryManager.Values)
				.WithValidations(validationHandler.Values)
				.WithSound(focused, @event)
				.WithSimpleDialogs(simpleDialogHolderManager.Values);
		}

		public async Task<FinalStateTransitionResponse> GetNextState(string task,
			int[] mandants,
			TerminalValue[] allValues,
			TerminalProperty[] allProperties,
			TerminalHistoryEvent[] events,
			TerminalValue[] postedValues,
			string focused,
			TerminalEventsEnum @event,
			string raiseObject,
			TerminalEventsEnum raiseEvent,
			TerminalEventsEnum dialogEvent,
			ReactionResponse[] reactionResponses)
		{
			await using var scope = provider.CreateAsyncScope();

			var serviceProvider = scope.ServiceProvider;

			using var readOnlySession = serviceProvider.GetRequiredService<IDbSession>();

			readOnlySession.Open(DbSessionReadWriteModesEnum.ReadOnly);

			var allForms = await navigations.GetAll(readOnlySession, task);

			var allDefaultProperties = allForms
				.SelectMany(TerminalPropertyExtensions.Map)
				.ToArray();

			defaultPropertiesHolderManager.Append(allDefaultProperties);

			eventHistoryManager.Initialize(events);

			postedValuesHolderManager.Append(allForms, postedValues);

			postedValuesHolderManager.SetInitialized();

			allValuesHolderManager.Append(allValues);

			allForms.ApplyProperties(allProperties)
				.ApplyValues(postedValuesHolderManager,
					allValues);

			var allReactions = await reactions.GetAll(readOnlySession, task);

			transitionFlowHolderManager.Clear();

			StateTransitionRequest request = new DialogStateTransitionRequest(allForms,
				focused,
				@event,
				raiseObject,
				raiseEvent,
				dialogEvent,
				allReactions,
				reactionResponses);

			StateTransitionResponse response;

			using var writeSession = serviceProvider.GetRequiredService<IDbSession>();

			writeSession.Open(DbSessionReadWriteModesEnum.Write);

			using var transaction = writeSession.BeginTransaction();

			do
			{
				response = await transitionManager.Next(writeSession, task, request);

				if (response is not NoActionFinalStateTransitionResponse &&
				    response is not ActionDialogFinalStateTransitionResponse)
				{
					if (response is FieldIntermediateStateTransitionResponse fieldIntermediate)
					{
						if (allForms.FindField(fieldIntermediate.Current) is ITerminalArithmeticExpressionField arithmeticField)
							arithmeticField.Evaluate();
					}

					eventHistoryManager.Append(response);

					var result = postedValuesHolderManager.Append(allForms, response);

					allForms.ApplyValues(result);
				}

				if (response is IntermediateStateTransitionResponse intermediateResponse)
				{
					request = intermediateResponse.GetRequest(allForms,
						allReactions,
						raiseObject,
						raiseEvent);
				}

			} while (response.IsFinal == false);

			await transaction.Commit();

			return ((FinalStateTransitionResponse)response)
				.WithTransitions(transitionFlowHolderManager.Values)
				.WithValues(postedValuesHolderManager.Values)
				.WithEvents(eventHistoryManager.Values)
				.WithValidations(validationHandler.Values)
				.WithSound(focused, @event)
				.WithSimpleDialogs(simpleDialogHolderManager.Values);
		}
	}
}
