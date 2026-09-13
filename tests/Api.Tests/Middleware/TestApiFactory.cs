using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StudyTime.Application.Exceptions;
using StudyTime.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using StudyTime.Api.Middleware;

namespace Api.Tests.Middleware;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITestExceptionStore, TestExceptionStore>();
        });

        builder.Configure(app =>
        {
            app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                TestExceptionEndpoints.MapTestExceptionEndpoints(endpoints);
            });
        });

    }
}

public interface ITestExceptionStore
{
    public Exception? Exception { get; set; }
}

public sealed class TestExceptionStore : ITestExceptionStore
{
    public Exception? Exception { get; set; }
}

public static class TestExceptionEndpoints
{
    public static void MapTestExceptionEndpoints(
        IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/test-exceptions/validation",
            (HttpContext _) => throw new ValidationException("Dados inválidos."));

        endpoints.MapGet(
            "/test-exceptions/not-found",
            (HttpContext _) => throw new NotFoundException("Recurso não encontrado."));

        endpoints.MapGet(
            "/test-exceptions/conflict",
            (HttpContext _) => throw new ConflictException("Conflito de estado."));

        endpoints.MapGet(
            "/test-exceptions/domain-rule",
            (HttpContext _) => throw DomainRuleViolationException.R03_StdWeekStudyTimeMustBePositive());

        endpoints.MapGet(
            "/test-exceptions/unhandled",
            (HttpContext _) => throw new InvalidOperationException("Falha inesperada."));
    }
}