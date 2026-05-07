using Molcom.Domain.Shared.Interfaces.Db;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Molcom.Domain.Terminal.Interfaces.Services;
using Molcom.Services.Core.Configurations;
using Molcom.Services.Terminal.Interfaces.Db;

namespace Molcom.Services.Terminal.Services;

public class AuthorizationService(
    IOptions<MocksOptions> mockOptions,
    IServiceProvider provider,
    ITerminalTaskDbGateway tasks)
    : TerminalService(mockOptions), IAuthorizationService
{

    public async Task<bool> HasAllowAccess(string task, int[] mandants)
    {
        if (IsTestTask(task))
            return true;

        await using var scope = provider.CreateAsyncScope();

        var serviceProvider = scope.ServiceProvider;

        using var session = serviceProvider.GetRequiredService<IDbSession>();

        session.Open(DbSessionReadWriteModesEnum.ReadOnly);

        var allTasks = await tasks.GetAll(session, mandants);

        return allTasks.Any(t => string.Equals(t.Code, task, StringComparison.CurrentCulture));
    }
}
