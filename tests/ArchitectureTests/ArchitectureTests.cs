using NetArchTest.Rules;

namespace ArchitectureTests;

public class DomainDependencyTests
{
    [Fact]
    public void Domain_KhongDuocPhuThuoc_EFCore_Hoac_AspNetCore()
    {
        var result = Types.InAssembly(typeof(TaskManagement.Domain.Placeholder).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_ChiDuocPhuThuoc_SharedKernel()
    {
        var domainAssembly = typeof(TaskManagement.Domain.Placeholder).Assembly;
        var allowed = new[] { "SharedKernel", "System", "netstandard", "System.Runtime", "mscorlib" };

        // Domain không được reference Application, Infrastructure, EventBus
        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOnAny("Application.Abstractions", "Infrastructure.Persistence", "EventBus", "MediatR", "FluentValidation")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain reference sai: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}

public class ModuleBoundaryTests
{
    [Fact]
    public void Application_KhongDuoc_Reference_TrucTiep_DomainCuaModuleKhac()
    {
        var result = Types.InAssembly(typeof(TaskManagement.Application.Placeholder).Assembly)
            .Should()
            .NotHaveDependencyOn("Projects.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"TaskManagement.Application không được reference Projects.Domain, chỉ được dùng Projects.Contracts. Vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Contracts_ChiDuocPhuThuoc_SharedKernel()
    {
        var result = Types.InAssembly(typeof(TaskManagement.Contracts.Placeholder).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "MediatR", "FluentValidation", "Infrastructure.Persistence", "EventBus")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Contracts vi phạm: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
