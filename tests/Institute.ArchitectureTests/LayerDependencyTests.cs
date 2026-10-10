using System.Reflection;
using Institute.Domain.Abstractions;
using NetArchTest.Rules;

namespace Institute.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string Application = "Institute.Application";
    private const string Infrastructure = "Institute.Infrastructure";
    private const string Api = "Institute.Api";

    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = global::Institute.Application.AssemblyReference.Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(global::Institute.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnOtherLayersOrFrameworks() =>
        AssertNoDependency(DomainAssembly, Application, Infrastructure, Api, "Microsoft.EntityFrameworkCore", "MediatR", "FluentValidation");

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi() =>
        AssertNoDependency(ApplicationAssembly, Infrastructure, Api);

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi() =>
        AssertNoDependency(InfrastructureAssembly, Api);

    [Fact]
    public void Handlers_ShouldBeInternalAndSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Handler").Or().HaveNameEndingWith("Handlers")
            .Should().NotBePublic().And().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void DomainEntities_ShouldBeSealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().Inherit(typeof(Entity<>))
            .And().AreNotAbstract()
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    private static void AssertNoDependency(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{assembly.GetName().Name} must not depend on {string.Join(", ", forbidden)}. Violations: "
            + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
