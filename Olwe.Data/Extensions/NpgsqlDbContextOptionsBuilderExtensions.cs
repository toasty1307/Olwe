using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Olwe.Data.Extensions.DateTimeTranslations;

namespace Olwe.Data.Extensions;

public static class NpgsqlDbContextOptionsBuilderExtensions
{
    public static NpgsqlDbContextOptionsBuilder UseDateTimeOffsetTranslations(
        this NpgsqlDbContextOptionsBuilder optionsBuilder)
    {
        ((optionsBuilder as IRelationalDbContextOptionsBuilderInfrastructure)
                .OptionsBuilder as IDbContextOptionsBuilderInfrastructure)
            .AddOrUpdateExtension(new DateTimeOffsetTranslationsOptions());

        return optionsBuilder;
    }
}