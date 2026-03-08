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
    /// <summary>Converts a database value to <see cref="DateOnly"/>.</summary>
    /// <param name="value">Raw value from the database reader.</param>
    /// <exception cref="DataException">Thrown when the value cannot be converted to DateOnly.</exception>
    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        string s when DateOnly.TryParse(s, out var parsed) => parsed,
        _ => throw new DataException($"Cannot convert value of type '{value.GetType()}' to DateOnly.")
    };

    /// <summary>Sets the <paramref name="parameter"/> value from a <see cref="DateOnly"/>.</summary>
    /// <param name="parameter">The DB parameter to configure.</param>
    /// <param name="value">The DateOnly value to write.</param>
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
