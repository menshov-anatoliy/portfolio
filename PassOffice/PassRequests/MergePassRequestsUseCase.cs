using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.Domain.PassOffice.Models.Enums;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using Molcom.Services.PassOffice.Security;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Services;

namespace Molcom.Services.PassOffice.UseCases.PassRequests;

/// <summary>UseCase: объединить несколько заявок в групповую заявку.</summary>
public sealed class MergePassRequestsUseCase(
	IPassRequestGateway gateway,
	IRouteTargetGateway routeTargetGateway,
	IDirectoryUserGateway directoryUserGateway,
	IPassRequestMutationPolicy mutationPolicy,
	IPassRequestProtocolService protocolService)
	: UseCaseBase<int[], PassRequestDto>
{
	private readonly PassRequestNameResolver _resolver = new(routeTargetGateway, directoryUserGateway);

	protected override string SuccessMessage => "Заявки успешно объединены";
	protected override string FailureMessage => "Не удалось объединить заявки";

	protected override Task ValidateAsync(int[] input)
	{
		if (input.Length < 2)
			Validator.AddGeneralError("Для объединения нужно минимум 2 заявки");

		return Task.CompletedTask;
	}

	protected override async Task<PassRequestDto?> HandleAsync(int[] input)
	{
		var requestIds = input
			.Select(PassRequestId.From)
			.ToList();

		var children = await gateway.GetByIdsAsync(requestIds);

		children = children
			.Where(r => r.IsGroup == false)
			.ToList();

		if (children.Count < 2)
		{
			Validator.AddGeneralError("Для группировки необходимо выбрать хотя бы две заявки, которые не являются группой для других");
			return null;
		}

		var hasFinalStatusRequests = children.Any(request =>
			mutationPolicy.CanMutate(request, PassRequestMutationOperation.Merge) == false);

		if (hasFinalStatusRequests)
		{
			Validator.AddGeneralError("Редактирование запрещено");
			return null;
		}

		// Удаляем исходные заявки и формируем групповую
		foreach (var child in children)
			await gateway.DeleteAsync(child.Id);

		var first = children[0];

		var group = new PassRequest
		{
			Id = PassRequestId.Empty,
			Number = PassRequestNumberGenerator.Next("ОБ"),
			GuestType = first.GuestType,
			PassType = first.PassType,
			Status = first.Status,
			Contractor = first.Contractor,
			LastName = first.LastName,
			FirstName = first.FirstName,
			MiddleName = first.MiddleName,
			VehiclePlate = first.VehiclePlate,
			PassDate = first.PassDate,
			PassDateFromUtc = first.PassDateFromUtc,
			PassDateToUtc = first.PassDateToUtc,
			IsGroup = true,
			Children = children,
		};

		await gateway.AddAsync(group);

		await protocolService.LogAsync(
			group.Id.Id,
			PassRequestProtocolEventType.Created,
			newStatus: group.Status,
			comment: "Создана групповая заявка при объединении");

		await protocolService.LogAsync(
			group.Id.Id,
			PassRequestProtocolEventType.Merged,
			previousStatus: group.Status,
			newStatus: group.Status,
			comment: "Объединение заявок",
			additionalData: new
			{
				ChildRequestIds = children.Select(child => child.Id.Id).ToArray(),
			});

		foreach (var child in children)
		{
			await protocolService.LogAsync(
				child.Id.Id,
				PassRequestProtocolEventType.Merged,
				previousStatus: child.Status,
				newStatus: child.Status,
				comment: "Заявка объединена в групповую",
				additionalData: new
				{
					GroupRequestId = group.Id.Id,
				});
		}

		var routeNames = await _resolver.ResolveRouteTargetNamesAsync([group]);
		var approverNames = await _resolver.ResolveApproverNamesAsync([group]);
		return PassRequestMapper.ToDto(group, routeNames, approverNames);
	}
}
