using System.Reflection;
using NetArchTest.Rules;

namespace BupaBooks.ArchitectureTests;

/// <summary>
/// The dependency rule, enforced by the build rather than by code review.
/// With three projects (no separate Domain project), these tests are what keep
/// the Domain folder pure and the Host honest.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly Application = typeof(BupaBooks.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(BupaBooks.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Host = typeof(Program).Assembly;

    private static void ShouldPass(TestResult result) =>
        result.IsSuccessful.ShouldBeTrue(
            "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []));

    [Fact]
    public void Domain_depends_on_nothing_outside_itself() =>
        ShouldPass(Types.InAssembly(Application)
            .That().ResideInNamespace("BupaBooks.Application.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "BupaBooks.Application.Abstractions",
                "BupaBooks.Application.Books",
                "BupaBooks.Application.Paging",
                "BupaBooks.Infrastructure",
                "BupaBooks.Host",
                "Microsoft.Extensions",
                "Microsoft.AspNetCore")
            .GetResult());

    [Fact]
    public void Application_does_not_know_about_transport_or_hosting() =>
        ShouldPass(Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOnAny(
                "BupaBooks.Infrastructure",
                "BupaBooks.Host",
                "Microsoft.AspNetCore",
                "System.Net.Http",
                "System.Text.Json",
                "Polly")
            .GetResult());

    [Fact]
    public void Infrastructure_does_not_depend_on_the_host() =>
        ShouldPass(Types.InAssembly(Infrastructure)
            .ShouldNot().HaveDependencyOnAny("BupaBooks.Host", "Microsoft.AspNetCore")
            .GetResult());

    [Fact]
    public void Only_the_composition_root_uses_infrastructure() =>
        // Program (global namespace) wires services; everything under BupaBooks.Host talks to Application only.
        ShouldPass(Types.InAssembly(Host)
            .That().ResideInNamespace("BupaBooks.Host")
            .ShouldNot().HaveDependencyOn("BupaBooks.Infrastructure")
            .GetResult());

    [Fact]
    public void Infrastructure_implementations_are_hidden() =>
        ShouldPass(Types.InAssembly(Infrastructure)
            .That().HaveNameEndingWith("Source")
            .ShouldNot().BePublic()
            .GetResult());
}
