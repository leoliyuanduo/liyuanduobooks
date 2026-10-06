using BupaBooks.Application.Books.GetBooks;
using Microsoft.Extensions.DependencyInjection;

namespace BupaBooks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetBooksHandler>();
        return services;
    }
}
