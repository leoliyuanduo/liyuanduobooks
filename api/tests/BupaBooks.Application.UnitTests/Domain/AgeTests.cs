using BupaBooks.Application.Domain;

namespace BupaBooks.Application.UnitTests.Domain;

public class AgeTests
{
    [Theory]
    [InlineData(0, AgeCategory.Child)]
    [InlineData(17, AgeCategory.Child)]
    [InlineData(18, AgeCategory.Adult)]
    [InlineData(19, AgeCategory.Adult)]
    [InlineData(120, AgeCategory.Adult)]
    public void Category_follows_the_18_and_over_rule(int years, AgeCategory expected) =>
        Age.FromYears(years).Category.ShouldBe(expected);

    [Fact]
    public void Negative_age_is_rejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Age.FromYears(-1));
}
