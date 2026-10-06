namespace BupaBooks.Application.Domain;

/// <summary>
/// A person's age in whole years. A value object: it cannot be negative, and it
/// owns the single rule that decides whether its holder is an adult.
/// </summary>
public readonly record struct Age
{
    /// <summary>Owners aged this or above are adults; 17 and below are children.</summary>
    public const int AdultMinimumAge = 18;

    public int Years { get; }

    private Age(int years) => Years = years;

    public static Age FromYears(int years)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(years);
        return new Age(years);
    }

    public AgeCategory Category => Years >= AdultMinimumAge ? AgeCategory.Adult : AgeCategory.Child;

    public override string ToString() => Years.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
