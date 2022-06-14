using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Olwe.Data.Extensions.DateTimeTranslations;

public class DateTimeOffsetTranslationsInfo
    : DbContextOptionsExtensionInfo
{
    public DateTimeOffsetTranslationsInfo(
        IDbContextOptionsExtension extension)
        : base(
            extension)
    {
    }

    public override bool IsDatabaseProvider
        => false;

    public override string LogFragment { get; }
        = "using DateTimeOffset translation extension";

    public override int GetServiceProviderHashCode()
        => 0;

    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
    {
        return false;
    }

    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }
}