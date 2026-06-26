using Molcom.Domain.PassOffice.Models.Queries;
using Molcom.Services.PassOffice.Helpers;
using Molcom.PassOffice.Facade.Models.Dto;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Mappers;
using System.Text.Json;

namespace Molcom.Services.PassOffice.UseCases.Passes;

/// <summary>UseCase: получить страницу пропусков с пагинацией и фильтрацией.</summary>
public sealed class SearchPassesUseCase(
	IPassGateway passGateway,
	IPassRequestGateway requestGateway)
	: UseCaseBase<PagedRequest, PagedResponse<PassListItemDto>>
{
	protected override string SuccessMessage => string.Empty;
	protected override string FailureMessage => "Не удалось получить список пропусков";

	protected override async Task<PagedResponse<PassListItemDto>?> HandleAsync(PagedRequest input)
	{
		var allPasses = await passGateway.GetAllAsync(new PagedRequest
		{
			Page = 1,
			PageSize = 0,
		});

		if (allPasses.Items.Count == 0)
		{
			return new PagedResponse<PassListItemDto>
			{
				Items = [],
				TotalRecords = 0,
				Page = 1,
				PageSize = input.PageSize,
			};
		}

		var requestIds = allPasses.Items
			.Select(pass => pass.RequestId)
			.Distinct()
			.ToList();

		var requests = await requestGateway.GetByIdsAsync(requestIds);
		var requestsById = requests.ToDictionary(request => request.Id.Id);

		var flatItems = allPasses.Items
			.Where(pass => requestsById.ContainsKey(pass.RequestId.Id))
			.Select(pass => PassMapper.ToListItemDto(pass, requestsById[pass.RequestId.Id]))
			.ToList();

		if (input.Filters is { Count: > 0 })
		{
			flatItems = ApplyFilters(flatItems, input.Filters).ToList();
		}

		var response = PaginationHelper.BuildPagedResponse(
			flatItems,
			input,
			item => item.Id,
			sortApplier: ApplySort);

		return response;
	}

	private static IEnumerable<PassListItemDto> ApplyFilters(
		IEnumerable<PassListItemDto> query,
		Dictionary<string, FilterValue> filters)
	{
		foreach (var (field, filter) in filters)
		{
			query = field.ToLowerInvariant() switch
			{
				"number" => PaginationHelper.ApplyStringFilter(query, item => item.Number, filter),
				"guesttype" => PaginationHelper.ApplyStringFilter(query, item => ToCamelCaseEnum(item.GuestType), filter),
				"passtype" => PaginationHelper.ApplyStringFilter(query, item => ToCamelCaseEnum(item.PassType), filter),
				"fullname" => PaginationHelper.ApplyStringFilter(query, item => item.FullName, filter),
				"vehicleplate" => PaginationHelper.ApplyStringFilter(query, item => item.VehiclePlate, filter),
				"passstatus" => PaginationHelper.ApplyStringFilter(query, item => ToCamelCaseEnum(item.PassStatus), filter),
				"arrivaldateutc" => PaginationHelper.ApplyStringFilter(query, item => item.ArrivalDateUtc?.ToString("yyyy-MM-dd") ?? string.Empty, filter),
				"contractor" => PaginationHelper.ApplyStringFilter(query, item => item.Contractor, filter),
				"validfromutc" => PaginationHelper.ApplyStringFilter(query, item => item.ValidFromUtc.ToString("O"), filter),
				"validtoutc" => PaginationHelper.ApplyStringFilter(query, item => item.ValidToUtc?.ToString("O") ?? string.Empty, filter),
				_ => query,
			};
		}

		return query;
	}

	private static IEnumerable<PassListItemDto> ApplySort(
		IEnumerable<PassListItemDto> query,
		string sortField,
		int sortOrder)
	{
		Func<PassListItemDto, object?> keySelector = sortField.ToLowerInvariant() switch
		{
			"number" => item => item.Number,
			"guesttype" => item => item.GuestType,
			"passtype" => item => item.PassType,
			"fullname" => item => item.FullName,
			"vehicleplate" => item => item.VehiclePlate,
			"passstatus" => item => item.PassStatus,
			"arrivaldateutc" => item => item.ArrivalDateUtc ?? DateTimeOffset.MinValue,
			"contractor" => item => item.Contractor,
			"validfromutc" => item => item.ValidFromUtc,
			"validtoutc" => item => item.ValidToUtc ?? DateTimeOffset.MaxValue,
			_ => item => item.Id,
		};

		return sortOrder == 1
			? query.OrderBy(keySelector)
			: query.OrderByDescending(keySelector);
	}

	private static string ToCamelCaseEnum<TEnum>(TEnum value)
		where TEnum : struct, Enum =>
		JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}

