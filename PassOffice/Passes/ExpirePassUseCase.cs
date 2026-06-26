using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.Domain.PassOffice.Models.Enums;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using Molcom.Services.PassOffice.UseCases.PassRequests;

namespace Molcom.Services.PassOffice.UseCases.Passes;

/// <summary>UseCase: перевести пропуск в состояние «Истёк».</summary>
public sealed class ExpirePassUseCase(
	IPassGateway passGateway,
	IPassRequestGateway requestGateway,
	IRouteTargetGateway routeTargetGateway,
	IDirectoryUserGateway directoryUserGateway)
	: UseCaseBase<int, PassDto>
{
	private readonly PassRequestNameResolver _resolver = new(routeTargetGateway, directoryUserGateway);

	protected override string SuccessMessage => "Срок действия пропуска завершён";
	protected override string FailureMessage => "Не удалось завершить срок действия пропуска";

	protected override Task ValidateAsync(int input)
	{
		if (input <= 0)
			Validator.AddGeneralError("Не указан идентификатор пропуска");

		return Task.CompletedTask;
	}

	protected override async Task<PassDto?> HandleAsync(int input)
	{
		var pass = await passGateway.GetByIdAsync(PassId.From(input));
		if (pass == null)
		{
			Validator.AddGeneralError("Пропуск не найден");
			return null;
		}

		if (!pass.TryTransitionTo(PassStatus.Expired, out var errorMessage))
		{
			Validator.AddGeneralError(errorMessage);
			return null;
		}

		await passGateway.UpdateAsync(pass);

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

