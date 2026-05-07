using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Molcom.Domain.Shared.Interfaces.Services;
using Molcom.Domain.Shared.Models.Identity;
using Molcom.Services.Core.Interfaces.Identity;
using Molcom.Services.Core.Interfaces.Security;
using Molcom.Services.Terminal.Extensions.Security;

namespace Molcom.Integration.IdentityServer.Managers;

public class AuthenticationIdentityAdapter(
	ISecurityContext securityContext,
	IHttpContextAccessor httpContextAccessor,
	IUserService userService,
	SignInManager signInManager)
	: ISimpleAuthenticationPort
{
	public async Task<AuthenticationResult> Login(string userName, string password)
	{
		bool? useCookies = true;
		bool? useSessionCookies = true;
		string? twoFactorCode = default;
		string? twoFactorRecoveryCode = default;

		var useCookieScheme = (useCookies == true) || (useSessionCookies == true);

		var isPersistent = (useCookies == true) && (useSessionCookies != true);

		signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

		var result = await signInManager.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure: true);

		if (result.RequiresTwoFactor)
		{
			if (!string.IsNullOrEmpty(twoFactorCode))
			{
				result = await signInManager.TwoFactorAuthenticatorSignInAsync(twoFactorCode, isPersistent, rememberClient: isPersistent);
			}
			else if (!string.IsNullOrEmpty(twoFactorRecoveryCode))
			{
				result = await signInManager.TwoFactorRecoveryCodeSignInAsync(twoFactorRecoveryCode);
			}
		}

		if (result.Succeeded == false)
		{
			if (result.IsLockedOut)
				return AuthenticateResult.Error("Пользователь заблокирован");

			if (result.IsNotAllowed)
				return AuthenticateResult.Error("Пользователь не может быть аутентифицирован");

			if (result.RequiresTwoFactor)
				return AuthenticateResult.Error("Необходима двухфакторная аутентификация");

			return AuthenticationResult.Error("Логин или пароль не найден");
		}

		// После успешной аутентификации обновляем контекст безопасности с текущим принципалом
		TerminalSecurityContextExtensions.AssignPrincipal(securityContext, httpContextAccessor.HttpContext);

		return AuthenticationResult.Success();
	}

	public async Task RefreshSignIn()
	{
		securityContext.ThrowIfNotAuthenticated(out var userId);

		var user = await userService.Get(userId);

		await signInManager.RefreshSignInAsync(user);

		// После успешной обновления принципала обновляем контекст безопасности с текущим принципалом
		TerminalSecurityContextExtensions.AssignPrincipal(securityContext, httpContextAccessor.HttpContext);
	}

	public async Task Logout()
	{
		await signInManager.SignOutAsync();

		TerminalSecurityContextExtensions.ClearPrincipal(securityContext);
	}
}