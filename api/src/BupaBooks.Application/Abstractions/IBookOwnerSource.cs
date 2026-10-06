using BupaBooks.Application.Domain;

namespace BupaBooks.Application.Abstractions;

/// <summary>
/// Port: where book owners come from. The Application layer owns this contract;
/// Infrastructure implements it (dependency inversion). Use cases never know an
/// HTTP API exists.
/// </summary>
public interface IBookOwnerSource
{
    /// <exception cref="BookSourceException">The source could not provide data.</exception>
    Task<IReadOnlyList<Owner>> GetOwnersAsync(CancellationToken cancellationToken);
}
