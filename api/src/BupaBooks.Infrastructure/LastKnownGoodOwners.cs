using BupaBooks.Infrastructure.BupaApi.Dtos;

namespace BupaBooks.Infrastructure;

/// <summary>
/// The most recent successful upstream response, kept in memory (singleton).
/// Lets the service degrade gracefully: during an upstream outage, users get
/// slightly old data instead of an error, and the circuit breaker can stay open
/// without users noticing. Thread-safe: the snapshot is immutable and swapped atomically.
/// </summary>
internal sealed class LastKnownGoodOwners(TimeProvider timeProvider)
{
    private sealed record Snapshot(BupaBookOwnerDto[] Owners, DateTimeOffset StoredAt);

    private Snapshot? _snapshot;

    public void Store(BupaBookOwnerDto[] owners) =>
        Volatile.Write(ref _snapshot, new Snapshot(owners, timeProvider.GetUtcNow()));

    public bool TryGet(TimeSpan maxAge, out BupaBookOwnerDto[] owners, out TimeSpan age)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        owners = [];
        age = TimeSpan.Zero;

        if (snapshot is null)
        {
            return false;
        }

        age = timeProvider.GetUtcNow() - snapshot.StoredAt;
        if (age > maxAge)
        {
            return false;
        }

        owners = snapshot.Owners;
        return true;
    }
}
