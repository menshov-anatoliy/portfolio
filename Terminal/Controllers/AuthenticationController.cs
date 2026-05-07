using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Molcom.Services.Terminal.Interfaces.Security;
using Molcom.Terminal.Facade.Models.Interfaces;
using Molcom.Terminal.Facade.Models.State.AllTasks.Responses;
using Molcom.Terminal.Facade.Models.State.Authentication.Requests;
using Molcom.Terminal.Facade.Services.Authentication;
using Molcom.Terminal.Facade.Services.Interfaces;
using Molcom.Terminal.Api.Infrastructure.Filter;

namespace Molcom.Terminal.Api.Controllers;

[ApiController]
[Route("api/terminal")]
[ServiceFilter<UserContextResultFilter>]
public class AuthenticationController(
		ITerminalLoginFacadeService loginService,
		ITerminalLogoutFacadeService logoutService,
		ITerminalSessionFacadeService sessionService,
		ITerminalSecurityContext securityContext,
		ILogger<WeatherForecastController> logger)
		: ControllerBase
{
	private readonly ILogger<WeatherForecastController> _logger = logger;

	[HttpPost("login")]
	public async Task<ITerminalResponse[]> Login([FromBody] ITerminalRequest request)
	{
		var isAuthenticated = securityContext.IsAuthenticated;

		if (isAuthenticated)
		{
			var allTasksResponse = new TerminalAllTasksStateGetResponse
			{
				Tasks = await sessionService.GetTasks()
			};

			return
			[
				allTasksResponse
			];
		}

		if (request is TerminalAuthenticationStateGetRequest authenticationGetRequest)
		{
			var authenticationGetResponse = await loginService.Get(authenticationGetRequest);

			return
			[
				authenticationGetResponse
			];
		}

		if (request is TerminalAuthenticationStatePostRequest authenticationPostRequest)
		{
			var authenticationPostResponse = await loginService.Post(authenticationPostRequest);

			if (authenticationPostResponse.IsAuthenticated == false)
				return
				[
					authenticationPostResponse
				];

			var allTasksResponse = new TerminalAllTasksStateGetResponse
			{
				Tasks = await sessionService.GetTasks()
			};

			return
			[
				authenticationPostResponse,
				allTasksResponse
			];
		}

		throw new NotImplementedException();
	}

	[HttpPost("login_manual")]
	public async Task<ITerminalResponse> Login(string login, [DataType(DataType.Password)] string password)
	{
		return await loginService.Post(new TerminalAuthenticationStatePostRequest
		{
			Login = login,
			Password = password
		});
	}

	[Authorize]
	[HttpPost("logout")]
	public async Task<ITerminalResponse[]> Logout()
	{
		await logoutService.Post();

		var authenticationGetResponse = await loginService.Get(new TerminalAuthenticationStateGetRequest());

		return
		[
			authenticationGetResponse
		];
	}
}