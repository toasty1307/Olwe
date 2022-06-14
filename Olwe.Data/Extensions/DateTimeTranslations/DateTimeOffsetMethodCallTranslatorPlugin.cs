using Microsoft.EntityFrameworkCore.Query;

namespace Olwe.Data.Extensions.DateTimeTranslations;

public class DateTimeOffsetMethodCallTranslatorPlugin
    : IMethodCallTranslatorPlugin
{
    public DateTimeOffsetMethodCallTranslatorPlugin(
        ISqlExpressionFactory sqlExpressionFactory)
    {
        Translators = new[]
        {
            new DateTimeOffsetMethodCallTranslator(sqlExpressionFactory)
        };
    }

    public IEnumerable<IMethodCallTranslator> Translators { get; }
}