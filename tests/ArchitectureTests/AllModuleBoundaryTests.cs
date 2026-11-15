using System.Reflection;

namespace ArchitectureTests;

public class AllModuleBoundaryTests
{
    private static readonly string[] Modules =
    [
        "Identity", "Projects", "TaskManagement", "Sprints", "Notifications", "RiskAI", "Dashboard"
    ];

    [Theory]
    [InlineData("Identity")]
    [InlineData("Projects")]
    [InlineData("TaskManagement")]
    [InlineData("Sprints")]
    [InlineData("Notifications")]
    [InlineData("RiskAI")]
    public void Domain_HasNoFrameworkOrOtherModuleDependencies(string module)
    {
        AssertReferences($"{module}.Domain", reference =>
            reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
            reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            reference is "MediatR" or "FluentValidation" or "EventBus" ||
            Modules.Any(other => other != module && reference.StartsWith(other + ".", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Projects")]
    [InlineData("TaskManagement")]
    [InlineData("Sprints")]
    [InlineData("Notifications")]
    [InlineData("RiskAI")]
    public void Contracts_HaveNoApplicationOrInfrastructureDependencies(string module)
    {
        AssertReferences($"{module}.Contracts", reference =>
            reference is "EventBus" or "MediatR" or "FluentValidation" ||
            reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            Modules.Any(other => reference == $"{other}.Domain" ||
                reference == $"{other}.Application" || reference == $"{other}.Infrastructure"));
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Projects")]
    [InlineData("TaskManagement")]
    [InlineData("Sprints")]
    [InlineData("Notifications")]
    [InlineData("RiskAI")]
    [InlineData("Dashboard")]
    public void Application_UsesOnlyForeignContracts(string module)
    {
        AssertReferences($"{module}.Application", reference =>
            reference == "Infrastructure.Persistence" ||
            Modules.Any(other => other != module &&
                (reference == $"{other}.Domain" || reference == $"{other}.Application" ||
                 reference == $"{other}.Infrastructure" || reference == $"{other}.Api")));
    }

    private static void AssertReferences(string assemblyName, Func<string, bool> forbidden)
    {
        var assembly = Assembly.Load(assemblyName);
        var violations = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(forbidden)
            .ToArray();
        Assert.True(violations.Length == 0, $"{assemblyName} references: {string.Join(", ", violations)}");
    }
}
