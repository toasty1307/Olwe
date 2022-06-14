using Microsoft.EntityFrameworkCore.Query;

namespace Olwe.Data.Extensions.DateTimeTranslations;

public class DateTimeOffsetMemberTranslatorPlugin
    : IMemberTranslatorPlugin
{
    public DateTimeOffsetMemberTranslatorPlugin(
        ISqlExpressionFactory sqlExpressionFactory)
    {
        Translators = new[]
        {
            new DateTimeOffsetMemberTranslator(sqlExpressionFactory)
        };
    }

    public IEnumerable<IMemberTranslator> Translators { get; }
}