using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using Molcom.Services.PassOffice.UseCases.PassRequests;

namespace Molcom.Services.PassOffice.UseCases.Passes;

/// <summary>UseCase: получить пропуск по идентификатору.</summary>
public sealed class GetPassByIdUseCase(
	IPassGateway passGateway,
	IPassRequestGateway requestGateway,
	IRouteTargetGateway routeTargetGateway,
	IDirectoryUserGateway directoryUserGateway)
	: UseCaseBase<int, PassDto?>
{
	private readonly PassRequestNameResolver _resolver = new(routeTargetGateway, directoryUserGateway);

	protected override string SuccessMessage => string.Empty;
	protected override string FailureMessage => "Пропуск не найден";

	protected override async Task<PassDto?> HandleAsync(int input)
	{
		var pass = await passGateway.GetByIdAsync(PassId.From(input));
		if (pass == null)
		{
			Validator.AddGeneralError(FailureMessage);
			return null;
		}

		var request = await requestGateway.GetByIdAsync(pass.RequestId);
		if (request == null)
		{
			Validator.AddGeneralError("Исходная заявка для пропуска не найдена");
			return null;
		}

		var routeNames = await _resolver.ResolveRouteTargetNamesAsync([request]);
		var approverNames = await _resolver.ResolveApproverNamesAsync([request]);
		var requestDto = PassRequestMapper.ToDto(request, routeNames, approverNames);
		return PassMapper.ToDto(pass, requestDto);
	}
}

