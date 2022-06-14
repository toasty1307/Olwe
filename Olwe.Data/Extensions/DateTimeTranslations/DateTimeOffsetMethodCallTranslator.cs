﻿using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace Olwe.Data.Extensions.DateTimeTranslations;

public class DateTimeOffsetMethodCallTranslator
    : IMethodCallTranslator
{
    public DateTimeOffsetMethodCallTranslator(
        ISqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
    }

#pragma warning disable EF1001 // Internal EF Core API usage.
    public SqlExpression? Translate(
        SqlExpression instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        => ((method.DeclaringType == typeof(DateTimeOffset))
            && (method.Name == nameof(DateTimeOffset.ToUniversalTime))
            && _sqlExpressionFactory is NpgsqlSqlExpressionFactory npgsqlSqlExpressionFactory)
            ? npgsqlSqlExpressionFactory.AtTimeZone(
                instance,
                npgsqlSqlExpressionFactory.Constant("UTC"),
                method.ReturnType)
            : null;
#pragma warning restore EF1001 // Internal EF Core API usage.

    private readonly ISqlExpressionFactory _sqlExpressionFactory;
}