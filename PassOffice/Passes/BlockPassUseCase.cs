using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.Domain.PassOffice.Models.Enums;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using Molcom.Services.PassOffice.Services;
using Molcom.Services.PassOffice.UseCases.PassRequests;

namespace Molcom.Services.PassOffice.UseCases.Passes;

/// <summary>UseCase: перевести пропуск в состояние «Заблокирован».</summary>
public sealed class BlockPassUseCase(
	IPassGateway passGateway,
	IPassRequestGateway requestGateway,
	IRouteTargetGateway routeTargetGateway,
	IDirectoryUserGateway directoryUserGateway,
	IPassRequestProtocolService protocolService)
	: UseCaseBase<int, PassDto>
{
	private readonly PassRequestNameResolver _resolver = new(routeTargetGateway, directoryUserGateway);

	protected override string SuccessMessage => "Пропуск заблокирован";
	protected override string FailureMessage => "Не удалось заблокировать пропуск";

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

		var previousStatus = pass.Status;
		if (!pass.TryTransitionTo(PassStatus.Blocked, out var errorMessage))
		{
			Validator.AddGeneralError(errorMessage);
			return null;
		}

		await passGateway.UpdateAsync(pass);
		await protocolService.LogAsync(
			pass.RequestId.Id,
			PassRequestProtocolEventType.BlockPass,
			previousPassStatus: previousStatus,
			newPassStatus: pass.Status,
			comment: "Операция блокировки пропуска",
			additionalData: null);

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

