using Molcom.DAL.Cache.Configurations;
using Molcom.DAL.SqlServer.Configurations;
using Molcom.Integration.Cryptography.Configurations;
using Molcom.Integration.IdentityServer.Configurations;
using Molcom.Integration.WindowsServer.Configurations;
using Molcom.Terminal.Api.Configurations;
using Molcom.Terminal.Api.Configurations.Services;
using Molcom.Terminal.Api.Infrastructure.Middleware.RedirectMiddleware;
using Molcom.Terminal.Api.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddAppConfig()
	.AddOptions()
	.AddMiddleware()
	.AddCryptography()
	.AddCookie()
	.AddFilters()
	.AddExceptionHandlers()
	.AddApplicationServices()
	.AddFacadeServices()
	.AddSecurity()
	.AddHost()
	.AddLogging()
	.AddControllers()
	.AddCors()
	.AddSwagger()
	.AddOpenApi()
	.AddSpa()

	// Migrations (только для Development)
	.AddMigrations()

	// Infrastructure
	.AddSqlServerLayer()
	.AddSqlServerTerminalGateways()
	.AddTerminalCacheLayer()
	.AddIdentity()
	.AddActiveDirectoryServices()
	.AddDnsServices();

var app = builder.Build();

app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Molcom.Terminal.Api v1"));
}

if (app.Environment.IsDevelopment() == false)
	app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(CorsConfiguration.AllowSpecificOrigins);

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<HttpClientHostContextMiddleware>();
app.UseMiddleware<SecurityContextRedirectMiddleware>();

app.UseMiddleware<ApplicationSessionRedirectMiddleware>();
app.UseMiddleware<TerminalSessionRedirectMiddleware>();

app.MapControllers();

app.UseStaticFiles();

app.UseSession();

// Настройка SPA должна быть последней
app.UseSpaApplication();

// Применение миграций (только для Development)
await app.UseMigrationsAsync();

await app.RunAsync();
