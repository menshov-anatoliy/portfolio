using Molcom.Domain.PassOffice.Models.Entities;
using Molcom.Domain.PassOffice.Models.Enums;
using Molcom.Services.PassOffice.Interfaces.Db;
using Molcom.Services.PassOffice.Services;

namespace Molcom.Services.PassOffice.UseCases.Passes;

/// <summary>UseCase: массово валидировать активные пропуска по сроку действия.</summary>
public sealed class ValidatePassesUseCase(
	IPassGateway passGateway,
	IPassRequestProtocolService protocolService)
	: UseCaseBase<int[], bool>
{
	protected override string SuccessMessage => "Валидация пропусков выполнена";
	protected override string FailureMessage => "Не удалось выполнить валидацию пропусков";

	protected override Task ValidateAsync(int[] input)
	{
		if (input.Length == 0)
			Validator.AddGeneralError("Не переданы идентификаторы пропусков");

		if (input.Any(id => id <= 0))
			Validator.AddGeneralError("Список идентификаторов пропусков содержит некорректные значения");

		return Task.CompletedTask;
	}

	protected override async Task<bool> HandleAsync(int[] input)
	{
		var ids = input
			.Distinct()
			.ToArray();

		var processedCount = 0;

		foreach (var id in ids)
		{
			var pass = await passGateway.GetByIdAsync(PassId.From(id));
			if (pass == null || pass.Status != PassStatus.Active)
				continue;

			var previousStatus = pass.Status;
			var statusChanged = false;

			if (pass.ValidToUtc.HasValue && pass.ValidToUtc.Value <= DateTimeOffset.UtcNow)
			{
				if (pass.TryTransitionTo(PassStatus.Expired, out _))
				{
					await passGateway.UpdateAsync(pass);
					statusChanged = true;
				}
			}

			await protocolService.LogAsync(
				pass.RequestId.Id,
				PassRequestProtocolEventType.ValidatePass,
				previousPassStatus: previousStatus,
				newPassStatus: pass.Status,
				comment: "Проверка срока действия пропуска",
				additionalData: new
				{
					StatusChanged = statusChanged,
					ValidationResult = statusChanged ? "Статус изменён" : "Статус не изменён",
				});

			processedCount++;
		}

		if (processedCount == 0)
		{
			Validator.AddGeneralError("Среди выбранных записей нет активных пропусков для валидации");
			return false;
		}

		return true;
	}
}



