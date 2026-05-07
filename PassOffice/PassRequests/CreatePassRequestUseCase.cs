using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.Domain.PassOffice.Models.Enums;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Services;

namespace Molcom.Services.PassOffice.UseCases.PassRequests;

/// <summary>UseCase: создать новую заявку на пропуск.</summary>
public sealed class CreatePassRequestUseCase(
	IPassRequestGateway gateway,
	IRouteTargetGateway routeTargetGateway,
	IDirectoryUserGateway directoryUserGateway,
	IPassRequestProtocolService protocolService)
	: UseCaseBase<SavePassRequestDto, PassRequestDto>
{
	private readonly PassRequestNameResolver _resolver = new(routeTargetGateway, directoryUserGateway);

	protected override string SuccessMessage => "Заявка успешно создана";
	protected override string FailureMessage => "Не удалось создать заявку";

	protected override Task ValidateAsync(SavePassRequestDto input)
	{
		PassRequestValidationRules.Apply(input, Validator);
		return Task.CompletedTask;
	}

	protected override async Task<PassRequestDto?> HandleAsync(SavePassRequestDto input)
	{
		var entity = PassRequestMapper.ToDomain(input);

		var additionalData = BuildCreatedRequestAdditionalData(entity);

		entity.Number = PassRequestNumberGenerator.Next("ЗП");

		entity.Status = PassRequestStatus.New;

		await gateway.AddAsync(entity);

		await protocolService.LogAsync(
			entity.Id.Id,
			PassRequestProtocolEventType.Created,
			newStatus: entity.Status,
			comment: "Создание заявки",
			additionalData: additionalData);

		var routeNames = await _resolver.ResolveRouteTargetNamesAsync([entity]);
		var approverNames = await _resolver.ResolveApproverNamesAsync([entity]);

		return PassRequestMapper.ToDto(entity, routeNames, approverNames);
	}

	private static object? BuildCreatedRequestAdditionalData(PassRequest entity)
	{
		var emptyEntity = PassRequestMapper.ToDomain(new SavePassRequestDto());

		var changes = PassRequestDiffBuilder
			.Build(emptyEntity, entity)
			.Where(change => !string.IsNullOrWhiteSpace(change.NewValue))
			.ToArray();

		return changes.Length == 0
			? null
			: new
			{
				Diff = changes,
			};
	}
}
