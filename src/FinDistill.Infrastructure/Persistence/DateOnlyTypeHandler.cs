using System.Data;
using Dapper;

namespace FinDistill.Infrastructure.Persistence;

/// <summary>
/// Dapper type handler for DateOnly ↔ DateTime conversion.
/// Required because Dapper does not natively support DateOnly.
/// Handles DateTime (SQL Server), DateOnly (Npgsql) and string inputs.
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        string s when DateOnly.TryParse(s, out var parsed) => parsed,
        _ => throw new DataException($"Cannot convert value of type '{value.GetType()}' to DateOnly.")
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
