using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Molcom.Domain.Shared.Interfaces.Db;
using Molcom.Domain.Terminal.Interfaces.Services;
using Molcom.Domain.Terminal.Models.Classifier;
using Molcom.Domain.Terminal.Models.Fields;
using Molcom.Domain.Terminal.Models.Values;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Terminal.Interfaces.Db;

namespace Molcom.Services.Terminal.Services;

public class ClassifierTerminalService(
	IOptions<MocksOptions> mockOptions,
	IServiceProvider provider,
	ITerminalClassifierDbGateway сlassifiers)
	: TerminalService(mockOptions), IClassifierTerminalService
{
	public async Task<ClassifierResultSet> Get(string task,
		string form,
		string block,
		string source,
		string ident,
		TerminalFieldValue[] values)
	{
		await using var scope = provider.CreateAsyncScope();

		var serviceProvider = scope.ServiceProvider;

		using var session = serviceProvider.GetRequiredService<IDbSession>();

		session.Open(DbSessionReadWriteModesEnum.ReadOnly);

		var classifierResultSet = await сlassifiers.Get(session,
			task,
			form,
			block,
			source,
			ident,
			values);

		// Если в классификаторе установлено ключевое и отображаемое значение, которых нет в наборе данных классификатора, набор данных дополняется этим значением
		var classifierResultColumns = classifierResultSet.Columns;
		var classifierResultRecords = classifierResultSet.Values;
		var classifierResultDataType = classifierResultSet.DataType;

		// Ищем в стеке значение текущего контрола с полной парой значений (ключ и значение) для добавление этого значения в набор данных классификатора
		var additionalRecords = values
			.Where(f => string.Equals(f.Code, $"{form}.{block}.{source}.{ident}", StringComparison.CurrentCultureIgnoreCase))
			.Where(f => f.Value != default)
			.Where(f => f.Display != default)
			.Where(f => ClassifierResultSetExtensions.Exists(classifierResultRecords, 
				classifierResultColumns, 
				TerminalValueFieldExtensions.ConvertTo(classifierResultDataType, f.Value)) == false)
			.Select(f => ClassifierResultSetExtensions.CreateRecord(classifierResultColumns, f))
			.ToArray();

		return new ClassifierResultSet(classifierResultSet.Display,
			classifierResultDataType,
			classifierResultColumns,
			classifierResultSet.Values
				.Union(additionalRecords)
				.ToArray());
	}
}
