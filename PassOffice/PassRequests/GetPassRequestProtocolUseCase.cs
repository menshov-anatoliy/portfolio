using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Mappers;
using Molcom.Services.PassOffice.Security;
using Molcom.Services.PassOffice.Services;
using Molcom.Services.PassOffice.Interfaces.Db;

namespace Molcom.Services.PassOffice.UseCases.PassRequests;

/// <summary>
/// UseCase: получить протокол событий заявки.
/// </summary>
public sealed class GetPassRequestProtocolUseCase(
	IPassRequestProtocolService protocolService,
	IDirectoryUserGateway directoryUserGateway,
	ICurrentUserAccessor currentUserAccessor)
	: UseCaseBase<int, List<PassRequestProtocolEventDto>>
{
	private const string ProtocolViewerRole = PassOfficeRoles.Management;

	protected override string SuccessMessage => "Протокол заявки получен";

	protected override string FailureMessage => "Не удалось получить протокол заявки";

	protected override Task ValidateAsync(int input)
	{
		if (input <= 0)
			Validator.AddGeneralError("Некорректный идентификатор заявки");

		return Task.CompletedTask;
	}

	protected override async Task<List<PassRequestProtocolEventDto>?> HandleAsync(int input)
	{
		var login = currentUserAccessor.Login;

		if (string.IsNullOrWhiteSpace(login))
		{
			Validator.AddGeneralError("Пользователь не аутентифицирован");
			return null;
		}

		var user = await directoryUserGateway.GetByLoginAsync(login);
		if (user == null)
		{
			Validator.AddGeneralError("Пользователь не найден");
			return null;
		}

		if (user.Groups.Contains(ProtocolViewerRole, StringComparer.OrdinalIgnoreCase) == false)
		{
			Validator.AddGeneralError("Недостаточно прав для просмотра протокола заявки");
			return null;
		}

		var events = await protocolService.GetByRequestIdAsync(input);
		
		return events.Select(PassRequestProtocolMapper.ToDto).ToList();
	}
}
