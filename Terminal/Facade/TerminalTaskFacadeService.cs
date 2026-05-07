using Microsoft.Extensions.Options;
using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces.Services;
using Molcom.Domain.Terminal.Interfaces.Services;
using Molcom.Domain.Terminal.Interfaces.Services.Sessions;
using Molcom.Domain.Terminal.Models.Enums;
using Molcom.Domain.Terminal.Models.StateTransition.Responses.Final;
using Molcom.Domain.Terminal.Models.Values;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Terminal.Interfaces.Security;
using Molcom.Shared.Facade.Services;
using Molcom.Terminal.Facade.Models.Classifiers;
using Molcom.Terminal.Facade.Models.Fields;
using Molcom.Terminal.Facade.Models.Forms;
using Molcom.Terminal.Facade.Models.State.Task.Requests;
using Molcom.Terminal.Facade.Models.State.Task.Requests.Choice;
using Molcom.Terminal.Facade.Models.State.Task.Requests.Edit;
using Molcom.Terminal.Facade.Models.State.Task.Responses;
using Molcom.Terminal.Facade.Models.State.Task.Responses.Choice;
using Molcom.Terminal.Facade.Models.State.Task.Responses.Dialog.Action;
using Molcom.Terminal.Facade.Models.State.Task.Responses.Edit;
using Molcom.Terminal.Facade.Models.State.Task.Responses.End;
using Molcom.Terminal.Facade.Models.State.Task.Responses.NoAccess;
using Molcom.Terminal.Facade.Services.Interfaces;
using Molcom.Terminal.Facade.Services.MapperHelpers;
using System.Text.RegularExpressions;
using Molcom.Domain.Shared.Models.Sessions;
using Molcom.Domain.Terminal.Models;
using Molcom.Services.Core.Interfaces.Security;
using Molcom.Services.Terminal.Extensions.Security;

namespace Molcom.Terminal.Facade.Services
{
	public class TerminalTaskFacadeService(
		ITerminalSecurityContext securityContext,
		INavigationTerminalService navigationService,
		IClassifierTerminalService classifierService,
		IAuthenticationService authenticationService,
		IApplicationSessionTerminalService applicationSessionService,
		ITaskSessionTerminalService taskSessionService,
		IAuthorizationService authorizationService,
		TerminalSourceCallInfo sourceCallInfo,
		IOptions<DirectoriesOptions> directoriesOptions)
		: FacadeService, ITerminalTaskFacadeService
	{
		public async Task<TerminalTaskStateGetResponse> Get(TerminalTaskStateGetRequest request)
		{
			securityContext.ThrowIfNotAuthenticated(out var userId);
			securityContext.ThrowIfApplicationSessionIdNotInitialized(out var applicationSessionId);

			var applicationSession = await applicationSessionService.GetActiveApplicationSessionByUserId(userId);

			applicationSession.ThrowIfNotInitialized();

			var activeMandants = applicationSession.ActiveMandants();

			if (await authorizationService.HasAllowAccess(request.Task, activeMandants) == false)
				return new NoAccessTerminalTaskStateGetResponse();

			await taskSessionService.OpenTaskSession(applicationSessionId, request.Task, activeMandants);

			sourceCallInfo.ResetSourceCall();

			await authenticationService.RefreshSignIn();

			var response = await navigationService.GetStartState(request.Task);

			if (response is ChangeFocusFinalStateTransitionResponse changeFocusResponse)
			{
				var dto = TerminalFormMapperHelper.Map(changeFocusResponse.Current);

				if (dto is TerminalEditFormDTO editFormDTO)
					return new TerminalTaskEditStateGetResponse
					{
						Task = request.Task,
						Current = editFormDTO,
						Focused = changeFocusResponse.Focused,
						Forms = changeFocusResponse.Forms
							.Select(TerminalFormMapperHelper.Map)
							.ToArray(),
						Values = changeFocusResponse.Values
							.Select(TerminalValueMapperHelper.Map)
							.ToArray(),
						Actions = changeFocusResponse.Events
							.Select(TerminalHistoryEventMapperHelper.Map)
							.ToArray(),
						Transitions = response.Transitions
							.Select(TerminalStateTransitionMapperHelper.Map)
							.ToArray(),
						Validations = response.Validations
							.Select(TerminalValidationMapperHelper.Map)
							.ToArray()
					};

				if (dto is TerminalChoiceFormDTO choiceFormDTO)
					return new TerminalTaskChoiceStateGetResponse
					{
						Task = request.Task,
						Current = choiceFormDTO,
						Focused = changeFocusResponse.Focused,
						Forms = changeFocusResponse.Forms
							.Select(TerminalFormMapperHelper.Map)
							.ToArray(),
						Values = changeFocusResponse.Values
							.Select(TerminalValueMapperHelper.Map)
							.ToArray(),
						Actions = changeFocusResponse.Events
							.Select(TerminalHistoryEventMapperHelper.Map)
							.ToArray(),
						Transitions = response.Transitions
							.Select(TerminalStateTransitionMapperHelper.Map)
							.ToArray(),
						Validations = response.Validations
							.Select(TerminalValidationMapperHelper.Map)
							.ToArray()
					};
			}

			if (response is EndTaskFinalStateTransitionResponse)
			{
				await taskSessionService.CloseTaskSessions(applicationSessionId, request.Task);

				await applicationSessionService.UpdateApplicationSessionFunctionalInstance(applicationSessionId,
					request.Task,
					default);

				await authenticationService.RefreshSignIn();

				return new TerminalTaskEndStateGetResponse
				{
					Values = response.Values
						.Select(TerminalValueMapperHelper.Map)
						.ToArray(),
					Actions = response.Events
						.Select(TerminalHistoryEventMapperHelper.Map)
						.ToArray(),
					Transitions = response.Transitions
						.Select(TerminalStateTransitionMapperHelper.Map)
						.ToArray()
				};
			}

			throw new NotImplementedException();
		}

		public async Task<TerminalTaskStatePostResponse> Post(TerminalTaskStatePostRequest request)
		{
			securityContext.ThrowIfNotAuthenticated(out _);
			securityContext.ThrowIfApplicationSessionIdNotInitialized(out _);

			var task = request.Task;

			if (request is TerminalTaskEditNavigationStatePostRequest navigationRequest)
			{
				var userAction = navigationRequest.UserAction;
				var focused = navigationRequest.Focused;
				var value = navigationRequest.Value.Value;
				var display = navigationRequest.Value.Display;

				var allValues = navigationRequest.Forms
					.SelectMany(TerminalValueMapperHelper.Map)
					.ToArray();

				var allProperties = navigationRequest.Forms
					.SelectMany(TerminalPropertyMapperHelper.Map)
					.ToArray();

				var actions = navigationRequest.Actions
					.Select(TerminalHistoryEventMapperHelper.Map)
					.ToArray();

				var postedValues = navigationRequest.Values
					.Select(TerminalValueMapperHelper.Map)
					.ToArray();

				var response = await navigationService.GetNextState(task,
					allValues,
					allProperties,
					actions,
					postedValues,
					value,
					display,
					focused,
					default,
					MappingExtensions.Map(userAction, e =>
					{
						if (e == TerminalTaskEditNavigationUserActionTypesEnum.FieldEnter)
							return TerminalEventsEnum.FieldEnter;

						if (e == TerminalTaskEditNavigationUserActionTypesEnum.FieldEscape)
							return TerminalEventsEnum.FieldEscape;

						if (e == TerminalTaskEditNavigationUserActionTypesEnum.ClearValue)
							return TerminalEventsEnum.ClearValue;

						throw new NotImplementedException();
					}));

				return await CreatePostResponse(request.Task, response);
			}

			if (request is TerminalTaskEditMenuCallStatePostRequest menuCallRequest)
			{
				var focused = menuCallRequest.Focused;
				var menu = menuCallRequest.Menu;
				var value = menuCallRequest.Value.Value;
				var display = menuCallRequest.Value.Display;

				var allValues = menuCallRequest.Forms
					.SelectMany(TerminalValueMapperHelper.Map)
					.ToArray();

				var allProperties = menuCallRequest.Forms
					.SelectMany(TerminalPropertyMapperHelper.Map)
					.ToArray();

				var actions = menuCallRequest.Actions
					.Select(TerminalHistoryEventMapperHelper.Map)
					.ToArray();

				var postedValues = menuCallRequest.Values
					.Select(TerminalValueMapperHelper.Map)
					.ToArray();

				var response = await navigationService.GetNextState(task,
					allValues,
					allProperties,
					actions,
					postedValues,
					value,
					display,
					focused,
					menu,
					TerminalEventsEnum.MenuCall);

				return await CreatePostResponse(request.Task, response);
			}

			if (request is TerminalTaskChoiceStatePostRequest choicePostRequest)
			{
				var userAction = choicePostRequest.UserAction;
				var focused = choicePostRequest.Focused;

				var allValues = choicePostRequest.Forms
					.SelectMany(TerminalValueMapperHelper.Map)
					.ToArray();

				var allProperties = choicePostRequest.Forms
					.SelectMany(TerminalPropertyMapperHelper.Map)
					.ToArray();

				var actions = choicePostRequest.Actions
					.Select(TerminalHistoryEventMapperHelper.Map)
					.ToArray();

				var postedValues = choicePostRequest.Values
					.Select(TerminalValueMapperHelper.Map)
					.ToArray();

				var response = await navigationService.GetNextState(task,
					allValues,
					allProperties,
					actions,
					postedValues,
					default,
					default,
					focused,
					focused,
					MappingExtensions.Map(userAction, value =>
					{
						if (value == TerminalTaskEditNavigationUserActionTypesEnum.FieldEnter)
							return TerminalEventsEnum.MenuCall;

						if (value == TerminalTaskEditNavigationUserActionTypesEnum.FieldEscape)
							return TerminalEventsEnum.FieldEscape;

						throw new NotImplementedException();
					}));

				return await CreatePostResponse(request.Task, response);
			}

			if (request is TerminalTaskEditDialogStatePostRequest dialogRequest)
			{
				var userAction = dialogRequest.UserAction;
				var raiseObject = dialogRequest.RaiseObject;
				var raiseEvent = TerminalEventsMapperHelper.Map(dialogRequest.RaiseEvent);
				var dialogEvent = TerminalEventsMapperHelper.Map(dialogRequest.DialogEvent);
				var focused = dialogRequest.Focused;

				var reactionResponses = dialogRequest.ReactionResponses
					.Select(TerminalReactionResponseMapperHelper.Map)
					.ToArray();

				var allValues = dialogRequest.Forms
					.SelectMany(TerminalValueMapperHelper.Map)
					.ToArray();

				var allProperties = dialogRequest.Forms
					.SelectMany(TerminalPropertyMapperHelper.Map)
					.ToArray();

				var actions = dialogRequest.Actions
					.Select(TerminalHistoryEventMapperHelper.Map)
					.ToArray();

				var postedValues = dialogRequest.Values
					.Select(TerminalValueMapperHelper.Map)
					.ToArray();

				var activeMandants = securityContext!.ActiveMandants;

				var response = await navigationService.GetNextState(task,
					activeMandants,
					allValues,
					allProperties,
					actions,
					postedValues,
					focused,
					MappingExtensions.Map(userAction, value =>
					{
						if (value == TerminalTaskEditDialogUserActionTypesEnum.DialogYes)
							return TerminalEventsEnum.DialogYes;

						if (value == TerminalTaskEditDialogUserActionTypesEnum.DialogNo)
							return TerminalEventsEnum.DialogNo;

						throw new NotImplementedException();
					}),
					raiseObject,
					raiseEvent,
					dialogEvent,
					reactionResponses);

				return await CreatePostResponse(request.Task, response);
			}

			if (request is TerminalTaskEditClassifierStatePostRequest classifierRequest)
			{
				var values = classifierRequest.Values
					.Select(TerminalValueMapperHelper.Map)
					.ToArray();

				var focused = classifierRequest.Focused;

				var focusedInfo = classifierRequest.Forms.FindField(focused);

				var classifierField = (TerminalClassifierFieldDTO)focusedInfo.Field;

				var classifierResultSet = await classifierService.Get(task,
					focusedInfo.Form.Code,
					focusedInfo.Block.Code,
					classifierField.Source,
					classifierField.Ident,
					values
						.OfType<TerminalFieldValue>()
						.ToArray());

				classifierField.IsInitialized = true;

				classifierField.Columns = classifierResultSet.Columns
					.Select(c => TerminalClassifierMapperHelper.Map(c, classifierResultSet.Values))
					.ToArray();

				classifierField.Records = classifierResultSet.Values
					.ToArray();

				return new TerminalTaskEditClassifierStatePostResponse
				{
					Task = task,
					UserAction = classifierRequest.UserAction,
					Classifier = new ClassifierResultDTO
					{
						Code = focused,
						Display = classifierResultSet.Display,
						Columns = classifierResultSet.Columns
							.Select(c => TerminalClassifierMapperHelper.Map(c, classifierResultSet.Values))
							.ToArray(),
						Values = classifierResultSet.Values,
						IsInitialized = true
					},
					Value = classifierRequest.Value,
					Forms = classifierRequest.Forms,
					Values = classifierRequest.Values,
					Actions = classifierRequest.Actions,
					Transitions = classifierRequest.Transitions,
					Sound = TerminalSoundTypesEnum.None
				};
			}

			throw new NotImplementedException();
		}

		private async Task<TerminalTaskStatePostResponse> CreatePostResponse(string task, FinalStateTransitionResponse response)
		{
			if (response is ChangeFocusFinalStateTransitionResponse changeFocusResponse)
			{
				var formDTO = TerminalFormMapperHelper.Map(changeFocusResponse.Current);

				if (formDTO is TerminalEditFormDTO editFormDTO)
					return new TerminalTaskEditNavigationStatePostResponse
					{
						Task = task,
						Current = editFormDTO,
						Focused = changeFocusResponse.Focused,
						Forms = changeFocusResponse.Forms
							.Select(TerminalFormMapperHelper.Map)
							.ToArray(),
						Values = response.Values
							.Select(TerminalValueMapperHelper.Map)
							.ToArray(),
						Actions = response.Events
							.Select(TerminalHistoryEventMapperHelper.Map)
							.ToArray(),
						Transitions = response.Transitions
							.Select(TerminalStateTransitionMapperHelper.Map)
							.ToArray(),
						Validations = response.Validations
							.Select(TerminalValidationMapperHelper.Map)
							.ToArray(),
						Sound = response.Sound,
						Dialogs = response.Dialogs
							.Select(TerminalSimpleDialogMapperHelper.Map)
							.ToArray()
					};

				if (formDTO is TerminalChoiceFormDTO choiceFormDTO)
					return new TerminalTaskChoiceStatePostResponse
					{
						Task = task,
						Current = choiceFormDTO,
						Focused = changeFocusResponse.Focused,
						Forms = changeFocusResponse.Forms
							.Select(TerminalFormMapperHelper.Map)
							.ToArray(),
						Values = response.Values
							.Select(TerminalValueMapperHelper.Map)
							.ToArray(),
						Actions = response.Events
							.Select(TerminalHistoryEventMapperHelper.Map)
							.ToArray(),
						Transitions = response.Transitions
							.Select(TerminalStateTransitionMapperHelper.Map)
							.ToArray(),
						Validations = response.Validations
							.Select(TerminalValidationMapperHelper.Map)
							.ToArray(),
						Sound = response.Sound,
						Dialogs = response.Dialogs
							.Select(TerminalSimpleDialogMapperHelper.Map)
							.ToArray()
					};
			}

			if (response is NoActionFinalStateTransitionResponse noActionResponse)
			{
				var formDTO = TerminalFormMapperHelper.Map(noActionResponse.Current);

				if (formDTO is TerminalEditFormDTO editFormDTO)
					return new TerminalTaskEditNavigationStatePostResponse
					{
						Task = task,
						Current = editFormDTO,
						Focused = noActionResponse.Focused,
						Forms = noActionResponse.Forms
							.Select(TerminalFormMapperHelper.Map)
							.ToArray(),
						Values = response.Values
							.Select(TerminalValueMapperHelper.Map)
							.ToArray(),
						Actions = response.Events
							.Select(TerminalHistoryEventMapperHelper.Map)
							.ToArray(),
						Transitions = response.Transitions
							.Select(TerminalStateTransitionMapperHelper.Map)
							.ToArray(),
						Validations = response.Validations
							.Select(TerminalValidationMapperHelper.Map)
							.ToArray(),
						Sound = response.Sound,
						Dialogs = response.Dialogs
							.Select(TerminalSimpleDialogMapperHelper.Map)
							.ToArray()
					};

				if (formDTO is TerminalChoiceFormDTO choiceFormDTO)
					return new TerminalTaskChoiceStatePostResponse
					{
						Task = task,
						Current = choiceFormDTO,
						Focused = noActionResponse.Focused,
						Forms = noActionResponse.Forms
							.Select(TerminalFormMapperHelper.Map)
							.ToArray(),
						Values = response.Values
							.Select(TerminalValueMapperHelper.Map)
							.ToArray(),
						Actions = response.Events
							.Select(TerminalHistoryEventMapperHelper.Map)
							.ToArray(),
						Transitions = response.Transitions
							.Select(TerminalStateTransitionMapperHelper.Map)
							.ToArray(),
						Sound = response.Sound,
						Dialogs = response.Dialogs
							.Select(TerminalSimpleDialogMapperHelper.Map)
							.ToArray()
					};
			}

			if (response is EndTaskFinalStateTransitionResponse)
			{
				securityContext.ThrowIfApplicationSessionIdNotInitialized(out var applicationSessionId);

				await taskSessionService.CloseTaskSessions(applicationSessionId, task);

				await applicationSessionService.UpdateApplicationSessionFunctionalInstance(applicationSessionId, task, default);

				await authenticationService.RefreshSignIn();

				return new TerminalTaskEndStatePostResponse
				{
					Task = task,
					Values = response.Values
						.Select(TerminalValueMapperHelper.Map)
						.ToArray(),
					Actions = response.Events
						.Select(TerminalHistoryEventMapperHelper.Map)
						.ToArray(),
					Transitions = response.Transitions
						.Select(TerminalStateTransitionMapperHelper.Map)
						.ToArray(),
					Sound = response.Sound,
					Dialogs = response.Dialogs
						.Select(TerminalSimpleDialogMapperHelper.Map)
						.ToArray()
				};
			}

			if (response is ActionDialogFinalStateTransitionResponse dialogResponse)
			{
				var postResponse = new TerminalTaskActionDialogStatePostResponse
				{
					Task = task,
					Focused = dialogResponse.Focused,
					DialogType = TerminalReactionResponseMapperHelper.Map(dialogResponse.DialogType),
					Message = dialogResponse.Message,
					RaiseObject = dialogResponse.RaiseObject,
					RaiseEvent = TerminalEventsMapperHelper.Map(dialogResponse.RaiseEvent),
					DialogEvent = TerminalEventsMapperHelper.Map(dialogResponse.DialogEvent),
					ReactionResponses = dialogResponse.ReactionResponses
						.Select(TerminalReactionResponseMapperHelper.Map)
						.ToArray(),
					Forms = dialogResponse.Forms
						.Select(TerminalFormMapperHelper.Map)
						.ToArray(),
					Values = response.Values
						.Select(TerminalValueMapperHelper.Map)
						.ToArray(),
					Actions = response.Events
						.Select(TerminalHistoryEventMapperHelper.Map)
						.ToArray(),
					Transitions = response.Transitions
						.Select(TerminalStateTransitionMapperHelper.Map)
						.ToArray(),
					Sound = response.Sound,
					Dialogs = response.Dialogs
						.Select(TerminalSimpleDialogMapperHelper.Map)
						.ToArray()
				};

				return postResponse;
			}

			throw new NotImplementedException();
		}
	}
}
