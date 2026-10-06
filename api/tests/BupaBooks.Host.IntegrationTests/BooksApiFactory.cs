using BupaBooks.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BupaBooks.Host.IntegrationTests;

/// <summary>The real Host (routing, validation, error handling, caching headers) with a fake upstream.</summary>
public sealed class BooksApiFactory : WebApplicationFactory<Program>
{
    public FakeBookOwnerSource Source { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBookOwnerSource>();
            services.AddSingleton<IBookOwnerSource>(Source);
        });
}
