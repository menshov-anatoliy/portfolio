using Microsoft.Extensions.Options;
using Molcom.DAL.Entities.Tables.Terminals.Users;
using Molcom.DAL.SqlServer.Repositories;
using Molcom.Domain.Shared.Extensions;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Domain.Shared.Models.Identity;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Core.Interfaces.Db.Gateways;
using NHibernate;
using NHibernate.Linq;

namespace Molcom.DAL.SqlServer.Gateways;

public class UserGateway(IOptions<MocksOptions> mockOptions,
	IUserRepository userRepository) : BaseGateway(mockOptions), IUserDbGateway
{
	public async Task<User> GetUser(IDbSession session, int id)
	{
		var internalSession = (ISession)session.InternalSession;

		var user = await userRepository.GetAsync(internalSession, id);

		return Map(user);
	}

	public async Task<User?> GetUser(IDbSession session, string login)
	{
		var internalSession = (ISession)session.InternalSession;

		var users = (await internalSession.Query<UserDb>()
				.Where(u => string.Equals(u.Login, login, StringComparison.CurrentCultureIgnoreCase))
				.Where(u => u.IsDeleted == false)
				.ToListAsync())
			.ToArray();

		if (users.Length == 0)
			return null;

		if (users.Length > 1)
			throw new ApplicationException("Найдено несколько пользователей с заданным логином. Обратитесь к администраторам.");

		var user = users[0];

		return Map(user);
	}

	public async Task<bool> IsUserExists(IDbSession session, string login)
	{
		var internalSession = (ISession)session.InternalSession;

		var users = (await internalSession.Query<UserDb>()
				.Where(u => string.Equals(u.Login, login, StringComparison.CurrentCultureIgnoreCase))
				.Where(u => u.IsDeleted == false)
				.ToListAsync())
			.ToArray();

		return users.Length == 1;
	}

	public async Task UpdateUserPassword(IDbSession session, User user)
	{
		var internalSession = (ISession)session.InternalSession;

		var existsUser = await userRepository.GetAsync(internalSession, user.Id);

		existsUser.ChangePasswordHash(user.PasswordHash);

		await userRepository.UpdateAsync(internalSession, existsUser);
	}

	private User Map(UserDb dbObject)
	{
		return new User
		{
			Id = dbObject.Id,
			UserName = dbObject.Login,
			PasswordHash = dbObject.Password,
			AuthenticationType = MappingExtensions.Map(dbObject.AuthenticationType, value =>
			{
				if (value == AuthenticationTypeDbEnum.Local)
					return AuthenticationTypeEnum.Local;

				if (value == AuthenticationTypeDbEnum.ActiveDirectory)
					return AuthenticationTypeEnum.ActiveDirectory;

				if (value == AuthenticationTypeDbEnum.Mixed)
					return AuthenticationTypeEnum.Mixed;

				throw new NotImplementedException();
			}),
			Groups = dbObject.UserGroups.Select(g => g.Code)
				.Distinct()
				.ToArray()
		};
	}
}
