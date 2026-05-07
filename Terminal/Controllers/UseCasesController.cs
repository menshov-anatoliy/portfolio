using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Molcom.Domain.Terminal.Models.Enums;
using Molcom.Services.Terminal.Interfaces.Security;
using Molcom.Terminal.Api.Infrastructure.Filter;
using Molcom.Terminal.Facade.Models.Interfaces;
using Molcom.Terminal.Facade.Models.State.AllMandants.Requests;
using Molcom.Terminal.Facade.Models.State.AllMandants.Responses;
using Molcom.Terminal.Facade.Models.State.AllTasks.Requests;
using Molcom.Terminal.Facade.Models.State.AllTasks.Responses;
using Molcom.Terminal.Facade.Models.State.Authentication.Requests;
using Molcom.Terminal.Facade.Models.State.Task.Enums;
using Molcom.Terminal.Facade.Models.State.Task.Requests;
using Molcom.Terminal.Facade.Models.State.Task.Responses.Dialog.Simple;
using Molcom.Terminal.Facade.Models.State.Task.Responses.NoAccess;
using Molcom.Terminal.Facade.Services.Authentication;
using Molcom.Terminal.Facade.Services.Interfaces;

namespace Molcom.Terminal.Api.Controllers;

[ApiController]
[Route("api/terminal")]
[ServiceFilter<HttpClientPathContextActionFilter>]
[ServiceFilter<UserContextResultFilter>]
public class UseCasesController(
		ITerminalLoginFacadeService loginService,
		ITerminalSessionFacadeService sessionService,
		ITerminalTaskFacadeService taskService,
		ITerminalSecurityContext securityContext,
		ILogger<WeatherForecastController> logger)
		: ControllerBase
{
	[Authorize]
	[HttpPost("postResponse")]
	public async Task<ITerminalResponse[]> GetResponse([FromBody] ITerminalRequest request)
	{
		var isAuthenticated = securityContext.IsAuthenticated;

		if (isAuthenticated == false)
		{
			var authenticationGetResponse = await loginService.Get(new TerminalAuthenticationStateGetRequest());

			return
			[
				authenticationGetResponse
			];
		}

		if (request is TerminalAllTasksStateGetRequest)
		{
			return
			[
				new TerminalAllTasksStateGetResponse
					{
						Tasks = await sessionService.GetTasks()
					}
			];
		}

		if (request is TerminalAllTasksStatePostRequest allTasksPostRequest)
		{
			var allTasksPostResponse = new TerminalAllTasksStatePostResponse
			{
				Task = allTasksPostRequest.Task
			};

			// Запуск нового терминального режима
			var taskGetResponse = await taskService.Get(new TerminalTaskStateGetRequest
			{
				Task = allTasksPostRequest.Task

			});

			return
			[
				allTasksPostResponse,
					taskGetResponse
			];
		}

		if (request is TerminalTaskStateGetRequest taskGetRequest)
		{
			// Запуск нового терминального режима
			var taskGetResponse = await taskService.Get(taskGetRequest);

            if (taskGetResponse is NoAccessTerminalTaskStateGetResponse)
                return
                [
                    new TerminalTaskSimpleDialogStateGetResponse
                    {
                        Caption = "Внимание!",
                        Text = "У вас нет доступа к задаче",
                        Sound = TerminalSoundTypesEnum.Attention
                    },

                    new TerminalAllTasksStateGetResponse
                    {
                        Tasks = await sessionService.GetTasks()
                    }
                ];

            if (taskGetResponse.TaskNavigation == TerminalNavigationTaskTypesEnum.End)
				return
				[
					new TerminalAllTasksStateGetResponse
						{
							Tasks = await sessionService.GetTasks()
						}
				];

			return
			[
				taskGetResponse
			];
		}

		if (request is TerminalTaskStatePostRequest taskPostRequest)
		{
			var taskPostResponse = await taskService.Post(taskPostRequest);

			if (taskPostResponse.TaskNavigation == TerminalNavigationTaskTypesEnum.End)
				return
				[
					new TerminalAllTasksStateGetResponse
						{
							Tasks = await sessionService.GetTasks()
						}
				];

			return
			[
				taskPostResponse
			];
		}

		if (request is TerminalAllMandantsStateGetRequest)
		{
			return
			[
				new TerminalAllMandantsStateGetResponse
					{
						Mandants = await sessionService.GetActiveMandants()
					}
			];
		}

		if (request is TerminalAllMandantsStatePostRequest allMandantsPostRequest)
		{
			await sessionService.SaveActiveMandants(allMandantsPostRequest.Mandants);

			return
			[
				new TerminalAllTasksStateGetResponse
					{
						Tasks = await sessionService.GetTasks()
					}
			];
		}

		throw new NotImplementedException();
	}
}
