using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Molcom.Domain.Shared.Models.Identity;
using Molcom.Services.Core.Interfaces.Cryptography;
using Molcom.Services.Core.Interfaces.Windows;

namespace Molcom.Integration.IdentityServer.Managers;

public class UserManager(
	IUserStore<User> store,
	IOptions<IdentityOptions> optionsAccessor,
	IPasswordHasher<User> passwordHasher,
	IEnumerable<IUserValidator<User>> userValidators,
	IEnumerable<IPasswordValidator<User>> passwordValidators,
	ILookupNormalizer keyNormalizer,
	IdentityErrorDescriber errors,
	IServiceProvider serviceProvider,
	IPasswordHasherPort userPasswordHasher,
	IActiveDirectoryGateway activeDirectoryGateway,
	ILogger<UserManager<User>> logger)
	: UserManager<User>(store,
		optionsAccessor,
		passwordHasher,
		userValidators,
		passwordValidators,
		keyNormalizer,
		errors,
		serviceProvider,
		logger)
{
	protected override async Task<PasswordVerificationResult> VerifyPasswordAsync(IUserPasswordStore<User> store, User user, string password)
	{
		var userName = user.UserName;

		if (user.AuthenticationType == AuthenticationTypeEnum.Local)
		{
			var hash = await store.GetPasswordHashAsync(user, CancellationToken).ConfigureAwait(false);

			if (hash == null)
				return PasswordVerificationResult.Failed;

			var isDecrypted = userPasswordHasher.TryDecrypt(hash, out var decryptedPassword);

			if (isDecrypted == false)
				return PasswordVerificationResult.Failed;

			if (string.Equals(decryptedPassword, password) == false)
				return PasswordVerificationResult.Failed;

			return PasswordVerificationResult.Success;
		}

		// Для успешной аутентификации требуется совпадение пары логин-пароль в AD и наличие логина в локальной БД
		if (user.AuthenticationType == AuthenticationTypeEnum.ActiveDirectory)
		{
			var isUserExists = activeDirectoryGateway.IsExists(userName, password);

			if (isUserExists)
				return PasswordVerificationResult.Success;

			return PasswordVerificationResult.Failed;
		}

		// Смешанная аутентификация: проверка пользователя через AD, обновление пароля в локальной БД
		if (user.AuthenticationType == AuthenticationTypeEnum.Mixed)
		{
			var hash = await store.GetPasswordHashAsync(user, CancellationToken).ConfigureAwait(false);

			if (hash == null)
				return PasswordVerificationResult.Failed;

			var newPassword = password;

			var isActiveDirectoryUserPasswordExists = activeDirectoryGateway.IsExists(userName, password);

			if (isActiveDirectoryUserPasswordExists == false)
			{
				// Проверяем, возможно пользователь воспользовался зашифрованным паролем
				var isDecrypted = userPasswordHasher.TryDecrypt(password, out var decryptedPassword);

				var isActiveDirectoryUserDecryptedPasswordExists = isDecrypted && activeDirectoryGateway.IsExists(userName, decryptedPassword!);

				newPassword = decryptedPassword;

				if (isActiveDirectoryUserDecryptedPasswordExists == false)
					return PasswordVerificationResult.Failed;
			}

			user.PasswordHash = userPasswordHasher.Encrypt(newPassword);

			await store.UpdateAsync(user, CancellationToken);

			return PasswordVerificationResult.Success;
		}

		throw new NotImplementedException();
	}
}