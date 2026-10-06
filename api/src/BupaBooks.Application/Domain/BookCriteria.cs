namespace BupaBooks.Application.Domain;

/// <summary>What the caller wants to see. A null <see cref="Format"/> means every format.</summary>
public sealed record BookCriteria(AgeCategory AgeCategory, BookFormat? Format = null);
