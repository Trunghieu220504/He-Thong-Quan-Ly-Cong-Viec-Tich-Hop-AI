using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>Chỉ dùng cho `dotnet ef` (design-time). Runtime sẽ đăng ký DbContext ở Host.</summary>
internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Port=5432;Database=pmai;Username=pmai;Password=pmai_Tuan2_2026";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(cs, o => o.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema))
            .Options;

        return new IdentityDbContext(options);
    }
}
