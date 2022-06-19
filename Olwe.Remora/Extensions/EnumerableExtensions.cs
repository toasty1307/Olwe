using System.Text;

namespace Olwe.Remora.Extensions;

public static class EnumerableExtensions
{
    public static string Join(this IEnumerable<string> source, string separator)
    {
        return source.Aggregate(new StringBuilder(), (sb, s) => sb.Append(s).Append(separator), sb => sb.ToString());
    }
}