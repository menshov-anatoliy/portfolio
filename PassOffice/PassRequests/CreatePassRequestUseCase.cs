using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.Domain.PassOffice.Models.Enums;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Services;
using Molcom.Services.PassOffice.Security;
using Molcom.Services.PassOffice.StateMachine;
using Molcom.Services.PassOffice.UseCases.PassRequests.Validation;

namespace Molcom.Services.PassOffice.UseCases.PassRequests;

/// <summary>UseCase: создать новую заявку на пропуск.</summary>
public sealed class CreatePassRequestUseCase(
	IPassRequestGateway gateway,
	IRouteTargetGateway routeTargetGateway,
	IPartnerGateway partnerGateway,
	IDirectoryUserGateway directoryUserGateway,
	IContractorGateway contractorGateway,
	ICurrentUserAccessor currentUserAccessor,
	IApproverDisplayNameResolver approverDisplayNameResolver,
	IApproverIdsProvider approverIdsProvider,
	IPassRequestProtocolService protocolService,
	IPassRequestValidationService validationService)
	: UseCaseBase<SavePassRequestDto, PassRequestDto>
{
	private readonly PassRequestNameResolver _resolver = new(routeTargetGateway, directoryUserGateway);

	protected override string SuccessMessage => "Заявка успешно создана";
	protected override string FailureMessage => "Не удалось создать заявку";

	protected override async Task ValidateAsync(SavePassRequestDto input)
	{
		await validationService.ValidateOnDataChange(input, Validator);
	}

	protected override async Task<PassRequestDto?> HandleAsync(SavePassRequestDto input)
	{
		var entity = PassRequestMapper.ToDomain(input);

		var autofillResult = await PassRequestCreationAutofillStrategies.TryApplyOnCreateAsync(
			entity,
			routeTargetGateway,
			partnerGateway,
			directoryUserGateway,
			contractorGateway,
			currentUserAccessor,
			approverDisplayNameResolver,
			approverIdsProvider,
			Validator);

		if (!autofillResult)
			return null;

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
