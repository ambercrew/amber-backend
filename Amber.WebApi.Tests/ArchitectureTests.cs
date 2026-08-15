using Amber.Application.Services;
using Amber.Domain.Common;
using Amber.Domain.Common.Interfaces;
using Amber.Infrastructure.Database.EntityConfigurations;
using NetArchTest.Rules;

namespace Amber.WebApi.Tests;

[TestClass]
public class ArchitectureTests
{
    private static Types GetCurrentTypes()
    {
        return Types.InAssemblies([
            typeof(Program).Assembly,
            typeof(IApplicationService).Assembly,
            typeof(ValueObject).Assembly,
            typeof(UserEntityTypeConfiguration).Assembly,
        ]);
    }

    [TestMethod]
    public void ControllersShouldNotReferenceServices()
    {
        // Arrange

        var serviceTypes = GetCurrentTypes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .Or()
            .ImplementInterface(typeof(IApplicationService))
            .GetTypes()
            .Select(t => t.FullName)
            .ToArray();

        // Act

        var result = GetCurrentTypes()
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOnAny(serviceTypes)
            .GetResult();

        // Assert

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: "controllers that directly access services:\n"
                    + string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? [])
            );
    }

    [TestMethod]
    public void ControllersShouldNotReferenceRepositories()
    {
        // Arrange

        var repositories = GetCurrentTypes()
            .That()
            .ImplementInterface(typeof(IRepository))
            .GetTypes()
            .Select(t => t.FullName)
            .ToArray();

        // Act

        var result = GetCurrentTypes()
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOnAny(repositories)
            .GetResult();

        // Assert

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: "controllers that directly access repositories:\n"
                    + string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? [])
            );
    }

    [TestMethod]
    public void DomainServicesShouldImplementIDomainService()
    {
        // Act

        var result = GetCurrentTypes()
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotInterfaces()
            .And()
            .ResideInNamespace("Amber.Domain")
            .Should()
            .ImplementInterface(typeof(IDomainService))
            .GetResult();

        // Assert

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: "domain types ending with 'Service' that do not implement IDomainService:\n"
                    + string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? [])
            );
    }

    [TestMethod]
    public void ApplicationServicesShouldImplementIApplicationService()
    {
        // Act

        var result = GetCurrentTypes()
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotInterfaces()
            .And()
            .ResideInNamespace("Amber.Application")
            .Should()
            .ImplementInterface(typeof(IApplicationService))
            .GetResult();

        // Assert

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: "application types ending with 'Service' that do not implement IApplicationService:\n"
                    + string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? [])
            );
    }

    [TestMethod]
    public void TypesEndingWithRepositoryShouldImplementIRepository()
    {
        // Act

        var result = GetCurrentTypes()
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreNotInterfaces()
            .Should()
            .ImplementInterface(typeof(IRepository))
            .GetResult();

        // Assert

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: "Types ending with 'Repository' that do not implement IRepository:\n"
                    + string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? [])
            );
    }
}
